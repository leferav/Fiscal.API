using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Fiscal.API.Controllers.Agentes;

[ApiController]
[Authorize]
[Route("api/agentes")]
public class AgentesFiscaisController : ControllerBase
{
    private readonly FiscalDbContext _context;

    public AgentesFiscaisController(FiscalDbContext context)
    {
        _context = context;
    }

    [HttpPost("vinculacao")]
    public async Task<IActionResult> GerarCodigoVinculacao()
    {
        if (!TryObterEmpresaId(out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var agora = DateTime.UtcNow;

        // Invalida códigos anteriores ainda disponíveis.
        var vinculacoesAnteriores =
            await _context.VinculacoesAgentesFiscais
                .Where(x =>
                    x.EmpresaId == empresaId &&
                    !x.Utilizado)
                .ToListAsync();

        foreach (var vinculacao in vinculacoesAnteriores)
        {
            vinculacao.Utilizado = true;
            vinculacao.UtilizadoEm = agora;
        }

        // 000000 até 999999.
        var numeroCodigo =
            RandomNumberGenerator.GetInt32(0, 1_000_000);

        var codigo =
            numeroCodigo.ToString("D6");

        var codigoHash =
            GerarHash(codigo);

        var expiraEm =
            agora.AddMinutes(10);

        var novaVinculacao =
            new VinculacaoAgenteFiscal
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                CodigoHash = codigoHash,
                ExpiraEm = expiraEm,
                Utilizado = false,
                CriadoEm = agora
            };

        _context.VinculacoesAgentesFiscais.Add(
            novaVinculacao);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigo,
            expiraEm,
            validadeMinutos = 10
        });
    }

    private bool TryObterEmpresaId(out Guid empresaId)
    {
        var empresaIdClaim =
            User.FindFirst("empresaId")?.Value;

        return Guid.TryParse(
            empresaIdClaim,
            out empresaId);
    }

    private static string GerarHash(string valor)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(valor));

        return Convert.ToHexString(bytes);
    }

    [AllowAnonymous]
    [HttpPost("vincular")]
    public async Task<IActionResult> VincularAgente(
    [FromBody] VincularAgenteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            return BadRequest(new
            {
                mensagem = "Código de vinculação não informado."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest(new
            {
                mensagem = "Nome do agente não informado."
            });
        }

        if (string.IsNullOrWhiteSpace(request.IdentificadorMaquina))
        {
            return BadRequest(new
            {
                mensagem = "Identificador da máquina não informado."
            });
        }

        var agora = DateTime.UtcNow;

        var codigoHash =
            GerarHash(request.Codigo.Trim());

        var vinculacao =
            await _context.VinculacoesAgentesFiscais
                .FirstOrDefaultAsync(x =>
                    x.CodigoHash == codigoHash &&
                    !x.Utilizado &&
                    x.ExpiraEm > agora);

        if (vinculacao is null)
        {
            return BadRequest(new
            {
                mensagem = "Código de vinculação inválido ou expirado."
            });
        }

        var agente =
            await _context.AgentesFiscais
                .FirstOrDefaultAsync(x =>
                    x.EmpresaId == vinculacao.EmpresaId &&
                    x.IdentificadorMaquina ==
                        request.IdentificadorMaquina);

        var credencial =
            Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

        var credencialHash =
            GerarHash(credencial);

        if (agente is null)
        {
            agente = new AgenteFiscal
            {
                Id = Guid.NewGuid(),
                EmpresaId = vinculacao.EmpresaId,
                Nome = request.Nome.Trim(),
                IdentificadorMaquina =
                    request.IdentificadorMaquina.Trim(),
                CredencialHash = credencialHash,
                Ativo = true,
                CriadoEm = agora,
                UltimaComunicacaoEm = agora
            };

            _context.AgentesFiscais.Add(agente);
        }
        else
        {
            agente.Nome = request.Nome.Trim();
            agente.CredencialHash = credencialHash;
            agente.Ativo = true;
            agente.AtualizadoEm = agora;
            agente.UltimaComunicacaoEm = agora;
        }

        vinculacao.Utilizado = true;
        vinculacao.UtilizadoEm = agora;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            agenteId = agente.Id,
            credencial,
            mensagem = "Fiscal.Agent vinculado com sucesso."
        });
    }


    [AllowAnonymous]
    [HttpPost("{agenteId:guid}/heartbeat")]
    public async Task<IActionResult> Heartbeat(
    Guid agenteId,
    [FromBody] HeartbeatAgenteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Credencial))
        {
            return Unauthorized(new
            {
                mensagem = "Credencial do agente não informada."
            });
        }

        var agente =
            await _context.AgentesFiscais
                .FirstOrDefaultAsync(x =>
                    x.Id == agenteId &&
                    x.Ativo);

        if (agente == null)
        {
            return Unauthorized(new
            {
                mensagem = "Agente não encontrado ou inativo."
            });
        }

        var credencialHash =
            GerarHash(request.Credencial);

        var hashRecebido =
            Encoding.UTF8.GetBytes(credencialHash);

        var hashArmazenado =
            Encoding.UTF8.GetBytes(agente.CredencialHash);

        if (hashRecebido.Length != hashArmazenado.Length ||
            !CryptographicOperations.FixedTimeEquals(
                hashRecebido,
                hashArmazenado))
        {
            return Unauthorized(new
            {
                mensagem = "Credencial do agente inválida."
            });
        }

        agente.UltimaComunicacaoEm =
            DateTime.UtcNow;

        agente.AtualizadoEm =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            status = "online",
            servidorEm = DateTime.UtcNow
        });
    }

    [HttpGet("meus")]
    public async Task<IActionResult> ListarMeusAgentes()
    {
        if (!TryObterEmpresaId(out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var agora = DateTime.UtcNow;
        var limiteOnline = agora.AddSeconds(-90);

        var agentes = await _context.AgentesFiscais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                id = x.Id,
                nome = x.Nome,
                identificadorMaquina = x.IdentificadorMaquina,
                ativo = x.Ativo,
                online = x.Ativo &&
                         x.UltimaComunicacaoEm != null &&
                         x.UltimaComunicacaoEm >= limiteOnline &&
                         x.UltimaComunicacaoEm <= agora,
                ultimaComunicacaoEm = x.UltimaComunicacaoEm,
                criadoEm = x.CriadoEm
            })
            .ToListAsync();

        return Ok(agentes);
    }


    [AllowAnonymous]
    [HttpGet("{agenteId:guid}/emissoes/pendente")]
    public async Task<IActionResult> BuscarEmissaoPendente(
        Guid agenteId,
        CancellationToken cancellationToken)
    {
        var credencial = Request.Headers["X-Agent-Credential"].ToString();

        if (string.IsNullOrWhiteSpace(credencial))
            return Unauthorized("Credencial não informada.");

        var agente = await _context.AgentesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == agenteId && x.Ativo,
                cancellationToken);

        if (agente == null)
            return Unauthorized("Agente não encontrado ou inativo.");

        var hashRecebido = Convert.FromHexString(
            GerarHash(credencial));

        byte[] hashArmazenado;

        try
        {
            hashArmazenado = Convert.FromHexString(
                agente.CredencialHash);
        }
        catch (FormatException)
        {
            return Unauthorized("Credencial inválida.");
        }

        if (hashRecebido.Length != hashArmazenado.Length ||
            !CryptographicOperations.FixedTimeEquals(
                hashRecebido, hashArmazenado))
        {
            return Unauthorized("Credencial inválida.");
        }

        // Dados do QR Code necessários ao Agent (após validar a credencial).
        var configuracaoFiscal = await _context.ConfiguracoesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == agente.EmpresaId, cancellationToken);

        if (configuracaoFiscal == null ||
            string.IsNullOrWhiteSpace(configuracaoFiscal.CscId) ||
            string.IsNullOrWhiteSpace(configuracaoFiscal.Csc))
            return Conflict("CSC/ID CSC não configurados para esta empresa.");

        await using var transacao =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        var nota = await _context.NotasFiscais
            .FromSqlInterpolated($"""
            SELECT *
            FROM notas_fiscais
            WHERE "EmpresaId" = {agente.EmpresaId}
              AND "Status" = {"PENDENTE_AGENT"}
              AND "Modelo" = {((short)65)}
              AND "XmlEnvio" IS NOT NULL
              AND "AgenteFiscalId" IS NULL
            ORDER BY "CriadoEm"
            LIMIT 1
            FOR UPDATE SKIP LOCKED
            """)
            .ToListAsync(cancellationToken);

        var notaReservada = nota.FirstOrDefault();

        if (notaReservada == null)
        {
            await transacao.CommitAsync(cancellationToken);
            return NoContent();
        }

        notaReservada.Status = "EM_PROCESSAMENTO";
        notaReservada.AgenteFiscalId = agenteId;
        notaReservada.ReservadaEm = DateTime.UtcNow;
        notaReservada.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return Ok(new
        {
            id = notaReservada.Id,
            empresaId = notaReservada.EmpresaId,
            modelo = notaReservada.Modelo,
            serie = notaReservada.Serie,
            numero = notaReservada.Numero,
            xml = notaReservada.XmlEnvio,
            cscId = configuracaoFiscal.CscId,
            csc = configuracaoFiscal.Csc
        });
    }


    [AllowAnonymous]
    [HttpPost("{agenteId:guid}/emissoes/{solicitacaoId:guid}/resultado")]
    public async Task<IActionResult> ReceberResultadoEmissao(
    Guid agenteId,
    Guid solicitacaoId,
    [FromBody] ResultadoEmissaoAgenteRequest request,
    CancellationToken cancellationToken)
    {
        var credencial = Request.Headers["X-Agent-Credential"].ToString();

        if (string.IsNullOrWhiteSpace(credencial))
            return Unauthorized("Credencial não informada.");

        var agente = await _context.AgentesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == agenteId && x.Ativo,
                cancellationToken);

        if (agente == null)
            return Unauthorized("Agente não encontrado ou inativo.");

        var hashRecebido = Convert.FromHexString(GerarHash(credencial));

        byte[] hashArmazenado;

        try
        {
            hashArmazenado = Convert.FromHexString(agente.CredencialHash);
        }
        catch (FormatException)
        {
            return Unauthorized("Credencial inválida.");
        }

        if (hashRecebido.Length != hashArmazenado.Length ||
            !CryptographicOperations.FixedTimeEquals(
                hashRecebido, hashArmazenado))
        {
            return Unauthorized("Credencial inválida.");
        }

        await using var transacao =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        var nota = await _context.NotasFiscais
            .FirstOrDefaultAsync(x =>
                x.Id == solicitacaoId &&
                x.EmpresaId == agente.EmpresaId &&
                x.AgenteFiscalId == agenteId,
                cancellationToken);

        if (nota == null)
            return NotFound("Solicitação não encontrada para este Agent.");

        if (nota.Status != "EM_PROCESSAMENTO")
            return Conflict("A solicitação não está em processamento.");

        // Não confiar somente no campo Sucesso.
        // A autorização precisa ser confirmada pelo retorno da SEFAZ.
        if (request.Sucesso && request.CStat == 100 &&
            !string.IsNullOrWhiteSpace(request.Protocolo) &&
            !string.IsNullOrWhiteSpace(request.XmlAutorizado))
        {
            nota.Status = "AUTORIZADA";
            nota.AutorizadoEm = DateTime.UtcNow;
        }
        else if (request.CStat.HasValue &&
                 request.CStat != 100 &&
                 !string.IsNullOrWhiteSpace(request.XmlRetorno))
        {
            nota.Status = "REJEITADA";
        }
        else
        {
            nota.Status = "RESULTADO_DESCONHECIDO";
        }

        nota.CStat = request.CStat;
        nota.XMotivo = request.Motivo;
        nota.ChaveAcesso = request.ChaveAcesso;
        nota.Protocolo = request.Protocolo;

        if (!string.IsNullOrWhiteSpace(request.XmlEnvio))
            nota.XmlEnvio = request.XmlEnvio;

        nota.XmlRetorno = request.XmlRetorno;
        nota.XmlAutorizado = request.XmlAutorizado;
        nota.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return Ok(new
        {
            notaId = nota.Id,
            status = nota.Status,
            cStat = nota.CStat,
            protocolo = nota.Protocolo
        });
    }
}

public class VincularAgenteRequest
{
    public string Codigo { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string IdentificadorMaquina { get; set; } = string.Empty;
}


public class HeartbeatAgenteRequest
{
    public string Credencial { get; set; } = string.Empty;
}

public class ResultadoEmissaoAgenteRequest
{
    public bool Sucesso { get; set; }
    public int? CStat { get; set; }
    public string? Motivo { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? Protocolo { get; set; }
    public string? XmlEnvio { get; set; }
    public string? XmlRetorno { get; set; }
    public string? XmlAutorizado { get; set; }
}