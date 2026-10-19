using Fiscal.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros;

[ApiController]
[Authorize]
[Route("api/cadastros/configuracoes-fiscais")]
public class ConfiguracoesFiscaisController : ControllerBase
{
    private readonly FiscalDbContext _context;

    public ConfiguracoesFiscaisController(
        FiscalDbContext context)
    {
        _context = context;
    }

    [HttpGet("minha")]
    public async Task<IActionResult> ObterMinhaConfiguracao()
    {
        if (!TryObterEmpresaId(out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var configuracao =
            await _context.ConfiguracoesFiscais
                .AsNoTracking()
                .Where(x => x.EmpresaId == empresaId)
                .Select(x => new
                {
                    x.Id,
                    x.Ambiente,
                    x.SerieNFe,
                    x.SerieNFCe,
                    x.ProximoNumeroNFe,
                    x.ProximoNumeroNFCe,

                    x.CscId,

                    // Não devolvemos o CSC para o frontend.
                    cscConfigurado =
                        !string.IsNullOrWhiteSpace(x.Csc)
                })
                .FirstOrDefaultAsync();

        if (configuracao is null)
        {
            return NotFound(new
            {
                mensagem =
                    "Configuração fiscal não encontrada."
            });
        }

        return Ok(configuracao);
    }

    [HttpPut("minha")]
    public async Task<IActionResult> AtualizarMinhaConfiguracao(
        [FromBody] AtualizarConfiguracaoFiscalRequest request)
    {
        if (!TryObterEmpresaId(out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        if (request.Ambiente != 1 &&
            request.Ambiente != 2)
        {
            return BadRequest(new
            {
                mensagem =
                    "Ambiente deve ser 1 (Produção) ou 2 (Homologação)."
            });
        }

        if (request.SerieNFe <= 0 ||
            request.SerieNFCe <= 0)
        {
            return BadRequest(new
            {
                mensagem =
                    "A série deve ser maior que zero."
            });
        }

        if (request.ProximoNumeroNFe <= 0 ||
            request.ProximoNumeroNFCe <= 0)
        {
            return BadRequest(new
            {
                mensagem =
                    "O próximo número deve ser maior que zero."
            });
        }

        var configuracao =
            await _context.ConfiguracoesFiscais
                .FirstOrDefaultAsync(
                    x => x.EmpresaId == empresaId
                );

        if (configuracao is null)
        {
            return NotFound(new
            {
                mensagem =
                    "Configuração fiscal não encontrada."
            });
        }

        configuracao.Ambiente =
            request.Ambiente;

        configuracao.SerieNFe =
            request.SerieNFe;

        configuracao.SerieNFCe =
            request.SerieNFCe;

        configuracao.ProximoNumeroNFe =
            request.ProximoNumeroNFe;

        configuracao.ProximoNumeroNFCe =
            request.ProximoNumeroNFCe;

        configuracao.CscId =
            string.IsNullOrWhiteSpace(request.CscId)
                ? null
                : request.CscId.Trim();

        /*
         * CSC:
         * - null = mantém o CSC atual
         * - preenchido = substitui o CSC atual
         */
        if (!string.IsNullOrWhiteSpace(request.Csc))
        {
            configuracao.Csc =
                request.Csc.Trim();
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            configuracao.Id,
            configuracao.Ambiente,
            configuracao.SerieNFe,
            configuracao.SerieNFCe,
            configuracao.ProximoNumeroNFe,
            configuracao.ProximoNumeroNFCe,
            configuracao.CscId,

            cscConfigurado =
                !string.IsNullOrWhiteSpace(
                    configuracao.Csc
                )
        });
    }

    private bool TryObterEmpresaId(
        out Guid empresaId)
    {
        var empresaIdClaim =
            User.FindFirst("empresaId")?.Value;

        return Guid.TryParse(
            empresaIdClaim,
            out empresaId
        );
    }
}

public class AtualizarConfiguracaoFiscalRequest
{
    public short Ambiente { get; set; }

    public int SerieNFe { get; set; }

    public int SerieNFCe { get; set; }

    public long ProximoNumeroNFe { get; set; }

    public long ProximoNumeroNFCe { get; set; }

    public string? CscId { get; set; }

    public string? Csc { get; set; }
}