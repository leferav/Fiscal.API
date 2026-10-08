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
    [HttpPost("{agenteId:guid}/certificado")]
    public async Task<IActionResult> SincronizarCertificado(
        Guid agenteId,
        [FromBody] SincronizarCertificadoAgenteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Credencial))
            return Unauthorized(new
            {
                mensagem = "Credencial do agente não informada."
            });

        var agente = await _context.AgentesFiscais
            .FirstOrDefaultAsync(x =>
                x.Id == agenteId && x.Ativo);

        if (agente is null)
            return Unauthorized(new
            {
                mensagem = "Agente não encontrado ou inativo."
            });

        var hashRecebido = Convert.FromHexString(
            GerarHash(request.Credencial));

        byte[] hashArmazenado;

        try
        {
            hashArmazenado = Convert.FromHexString(
                agente.CredencialHash);
        }
        catch (FormatException)
        {
            return Unauthorized(new
            {
                mensagem = "Credencial do agente inválida."
            });
        }

        if (hashRecebido.Length != hashArmazenado.Length ||
            !CryptographicOperations.FixedTimeEquals(
                hashRecebido, hashArmazenado))
        {
            return Unauthorized(new
            {
                mensagem = "Credencial do agente inválida."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Thumbprint) ||
            request.ValidoDe == null ||
            request.ValidoAte == null ||
            request.ValidoAte <= request.ValidoDe)
        {
            return BadRequest(new
            {
                mensagem = "Metadados do certificado inválidos."
            });
        }

        var certificado = await _context.CertificadosDigitais
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == agente.EmpresaId);

        var agora = DateTime.UtcNow;

        if (certificado is null)
        {
            certificado = new CertificadoDigital
            {
                Id = Guid.NewGuid(),
                EmpresaId = agente.EmpresaId,
                CriadoEm = agora
            };

            _context.CertificadosDigitais.Add(certificado);
        }

        certificado.TipoArmazenamento =
            Fiscal.API.Models.Enums.TipoArmazenamentoCertificado.Local;

        certificado.Titular = request.Titular?.Trim();
        certificado.Cnpj = request.Cnpj?.Trim();
        certificado.Emissor = request.Emissor?.Trim();
        certificado.Thumbprint = request.Thumbprint.Trim();
        certificado.ValidoDe = request.ValidoDe;
        certificado.ValidoAte = request.ValidoAte;
        certificado.Ativo = true;
        certificado.AtualizadoEm = agora;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Metadados do certificado sincronizados com sucesso.",
            certificado.Id,
            certificado.EmpresaId,
            certificado.AtualizadoEm
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


public class SincronizarCertificadoAgenteRequest
{
    public string Credencial { get; set; } = string.Empty;
    public string? Titular { get; set; }
    public string? Cnpj { get; set; }
    public string? Emissor { get; set; }
    public string? Thumbprint { get; set; }
    public DateTime? ValidoDe { get; set; }
    public DateTime? ValidoAte { get; set; }
}
