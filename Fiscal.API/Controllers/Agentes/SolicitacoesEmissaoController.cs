
using Fiscal.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Fiscal.API.Controllers.Agentes;

[ApiController]
[Route("api/agentes")]
public class SolicitacoesEmissaoController : ControllerBase
{
    private readonly FiscalDbContext _context;

    public SolicitacoesEmissaoController(FiscalDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpPost("{agenteId:guid}/solicitacoes/proxima")]
    public async Task<IActionResult> ObterProximaSolicitacao(
        Guid agenteId,
        [FromBody] ProximaSolicitacaoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Credencial))
            return Unauthorized(new
            {
                mensagem = "Credencial não informada."
            });

        var agente = await _context.AgentesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == agenteId && x.Ativo,
                cancellationToken);

        if (agente == null)
            return Unauthorized(new
            {
                mensagem = "Agent não encontrado ou inativo."
            });

        byte[] hashArmazenado;

        try
        {
            hashArmazenado =
                Convert.FromHexString(agente.CredencialHash);
        }
        catch (FormatException)
        {
            return Unauthorized();
        }

        var hashRecebido = SHA256.HashData(
            Encoding.UTF8.GetBytes(request.Credencial));

        if (hashRecebido.Length != hashArmazenado.Length ||
            !CryptographicOperations.FixedTimeEquals(
                hashRecebido, hashArmazenado))
        {
            return Unauthorized(new
            {
                mensagem = "Credencial inválida."
            });
        }

        var tentativaId = Guid.NewGuid();

        // Reserva atômica: somente uma chamada poderá
        // alterar a mesma solicitação PENDENTE.
        var solicitacao = await _context.Database
            .SqlQuery<SolicitacaoReservadaDto>($"""
                UPDATE solicitacoes_emissao
                SET "Status" = 'PROCESSANDO',
                    "AgenteId" = {agenteId},
                    "TentativaId" = {tentativaId},
                    "IniciadoEm" = NOW(),
                    "ReservaExpiraEm" = NOW() + INTERVAL '5 minutes',
                    "AtualizadoEm" = NOW()
                WHERE "Id" = (
                    SELECT "Id"
                    FROM solicitacoes_emissao
                    WHERE "EmpresaId" = {agente.EmpresaId}
                      AND "Status" = 'PENDENTE'
                    ORDER BY "CriadoEm", "Id"
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                )
                RETURNING
                    "Id",
                    "EmpresaId",
                    "TentativaId",
                    "Modelo",
                    "Ambiente",
                    "Serie",
                    "Numero",
                    "PayloadJson"::text AS "PayloadJson"
                """)
            .ToListAsync(cancellationToken);

        var proxima = solicitacao.FirstOrDefault();

        if (proxima == null)
            return NoContent();

        return Ok(proxima);
    }


    [AllowAnonymous]
    [HttpPost("{agenteId:guid}/solicitacoes/{solicitacaoId:guid}/resultado")]
    public async Task<IActionResult> RegistrarResultado(
        Guid agenteId,
        Guid solicitacaoId,
        [FromBody] ResultadoSolicitacaoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Credencial))
            return Unauthorized(new
            {
                mensagem = "Credencial não informada."
            });

        var agente = await _context.AgentesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == agenteId && x.Ativo,
                cancellationToken);

        if (agente == null)
            return Unauthorized(new
            {
                mensagem = "Agent não encontrado ou inativo."
            });

        byte[] hashArmazenado;

        try
        {
            hashArmazenado =
                Convert.FromHexString(agente.CredencialHash);
        }
        catch (FormatException)
        {
            return Unauthorized();
        }

        var hashRecebido = SHA256.HashData(
            Encoding.UTF8.GetBytes(request.Credencial));

        if (hashRecebido.Length != hashArmazenado.Length ||
            !CryptographicOperations.FixedTimeEquals(
                hashRecebido, hashArmazenado))
        {
            return Unauthorized(new
            {
                mensagem = "Credencial inválida."
            });
        }

        if (request.TentativaId == Guid.Empty)
            return BadRequest(new
            {
                mensagem = "TentativaId inválido."
            });

        var status = request.Status?.Trim().ToUpperInvariant();

        var statusPermitidos = new[]
        {
        "AUTORIZADA",
        "REJEITADA",
        "ERRO",
        "RESULTADO_DESCONHECIDO"
    };

        if (status == null || !statusPermitidos.Contains(status))
            return BadRequest(new
            {
                mensagem = "Status de resultado inválido."
            });

        if (status == "AUTORIZADA" &&
            (request.CStat != 100 ||
             string.IsNullOrWhiteSpace(request.Protocolo) ||
             string.IsNullOrWhiteSpace(request.ChaveAcesso) ||
             request.ChaveAcesso.Length != 44 ||
             !request.ChaveAcesso.All(char.IsDigit) ||
             string.IsNullOrWhiteSpace(request.XmlAutorizado)))
        {
            return BadRequest(new
            {
                mensagem = "Dados de autorização incompletos."
            });
        }

        if (status == "REJEITADA" &&
            (!request.CStat.HasValue ||
             request.CStat == 100 ||
             string.IsNullOrWhiteSpace(request.XMotivo)))
        {
            return BadRequest(new
            {
                mensagem = "Dados da rejeição incompletos."
            });
        }

        if (status == "ERRO" &&
            string.IsNullOrWhiteSpace(request.Erro))
        {
            return BadRequest(new
            {
                mensagem = "Informe o erro ocorrido antes da transmissão."
            });
        }

        if (status == "RESULTADO_DESCONHECIDO" &&
            string.IsNullOrWhiteSpace(request.Erro))
        {
            return BadRequest(new
            {
                mensagem = "Informe o motivo do resultado desconhecido."
            });
        }

        var linhasAfetadas =
            await _context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE solicitacoes_emissao
            SET "Status" = {status},
                "ChaveAcesso" = {request.ChaveAcesso},
                "Protocolo" = {request.Protocolo},
                "CStat" = {request.CStat},
                "XMotivo" = {request.XMotivo},
                "XmlAutorizado" = {request.XmlAutorizado},
                "Erro" = {request.Erro},
                "FinalizadoEm" = NOW(),
                "AtualizadoEm" = NOW()
            WHERE "Id" = {solicitacaoId}
              AND "EmpresaId" = {agente.EmpresaId}
              AND "AgenteId" = {agenteId}
              AND "TentativaId" = {request.TentativaId}
              AND "Status" = 'PROCESSANDO'
            """, cancellationToken);

        if (linhasAfetadas == 0)
        {
            return Conflict(new
            {
                mensagem = "Solicitação não encontrada, " +
                           "já finalizada ou tentativa inválida."
            });
        }

        return Ok(new
        {
            mensagem = "Resultado registrado com sucesso.",
            solicitacaoId,
            tentativaId = request.TentativaId,
            status
        });
    }


}

public class ProximaSolicitacaoRequest
{
    public string Credencial { get; set; } = string.Empty;
}

public class SolicitacaoReservadaDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? TentativaId { get; set; }
    public short Modelo { get; set; }
    public short Ambiente { get; set; }
    public int Serie { get; set; }
    public int Numero { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
}

public class ResultadoSolicitacaoRequest
{
    public string Credencial { get; set; } = string.Empty;

    public Guid TentativaId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ChaveAcesso { get; set; }

    public string? Protocolo { get; set; }

    public int? CStat { get; set; }

    public string? XMotivo { get; set; }

    public string? XmlAutorizado { get; set; }

    public string? Erro { get; set; }
}

