using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Requests.Cadastros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros;

[ApiController]
[Authorize]
[Route("api/cadastros/empresas")]
public class EmpresasController : ControllerBase
{
    private readonly FiscalDbContext _context;

    public EmpresasController(FiscalDbContext context)
    {
        _context = context;
    }

    /* ============================================================
       EMPRESA DA SESSÃO
       ============================================================ */

    private bool TentarObterEmpresaId(out Guid empresaId)
    {
        empresaId = Guid.Empty;

        var empresaIdClaim =
            User.FindFirst("empresaId")?.Value;

        if (string.IsNullOrWhiteSpace(empresaIdClaim))
            return false;

        return Guid.TryParse(
            empresaIdClaim,
            out empresaId
        );
    }

    /* ============================================================
       GET - MINHA EMPRESA
       GET /api/cadastros/empresas/minha
       ============================================================ */

    [HttpGet("minha")]
    public async Task<IActionResult> ObterMinhaEmpresa()
    {
        if (!TentarObterEmpresaId(out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem =
                    "Empresa não identificada no token."
            });
        }

        var empresa = await _context.Empresas
            .AsNoTracking()
            .Where(x =>
                x.Id == empresaId &&
                x.Ativo)
            .Select(x => new
            {
                x.Id,
                x.Cnpj,
                x.RazaoSocial,
                x.NomeFantasia,
                x.InscricaoEstadual,

                x.Logradouro,
                x.Numero,
                x.Bairro,

                x.CodigoMunicipio,
                x.Municipio,
                x.Uf,
                x.Cep,

                x.Crt,
                x.Ativo,
                x.CriadoEm,

                ConfiguracaoFiscal =
                    x.ConfiguracaoFiscal == null
                        ? null
                        : new
                        {
                            x.ConfiguracaoFiscal.Id,
                            x.ConfiguracaoFiscal.Ambiente,
                            x.ConfiguracaoFiscal.SerieNFCe,
                            x.ConfiguracaoFiscal.ProximoNumeroNFCe
                        }
            })
            .FirstOrDefaultAsync();

        if (empresa == null)
        {
            return NotFound(new
            {
                mensagem =
                    "Empresa não encontrada ou inativa."
            });
        }

        return Ok(empresa);
    }

    /* ============================================================
       PUT - MINHA EMPRESA
       PUT /api/cadastros/empresas/minha
       ============================================================ */

    [HttpPut("minha")]
    public async Task<IActionResult> AtualizarMinhaEmpresa(
        [FromBody] CadastrarEmpresaRequest request)
    {
        if (!TentarObterEmpresaId(out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem =
                    "Empresa não identificada no token."
            });
        }

        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(x =>
                x.Id == empresaId &&
                x.Ativo);

        if (empresa == null)
        {
            return NotFound(new
            {
                mensagem =
                    "Empresa não encontrada ou inativa."
            });
        }

        /* --------------------------------------------------------
           VALIDAÇÃO CNPJ
           -------------------------------------------------------- */

        if (string.IsNullOrWhiteSpace(request.Cnpj))
        {
            return BadRequest(new
            {
                mensagem = "Informe o CNPJ."
            });
        }

        if (string.IsNullOrWhiteSpace(
            request.RazaoSocial))
        {
            return BadRequest(new
            {
                mensagem =
                    "Informe a razão social."
            });
        }

        var cnpj = new string(
            request.Cnpj
                .Where(char.IsDigit)
                .ToArray()
        );

        var cnpjExistente =
            await _context.Empresas.AnyAsync(x =>
                x.Cnpj == cnpj &&
                x.Id != empresaId);

        if (cnpjExistente)
        {
            return BadRequest(new
            {
                mensagem =
                    "Já existe outra empresa cadastrada com este CNPJ."
            });
        }

        /* --------------------------------------------------------
           ATUALIZAÇÃO
           -------------------------------------------------------- */

        empresa.Cnpj = cnpj;

        empresa.RazaoSocial =
            request.RazaoSocial.Trim();

        empresa.NomeFantasia =
            string.IsNullOrWhiteSpace(
                request.NomeFantasia)
                ? null
                : request.NomeFantasia.Trim();

        empresa.InscricaoEstadual =
            string.IsNullOrWhiteSpace(
                request.InscricaoEstadual)
                ? null
                : request.InscricaoEstadual.Trim();

        empresa.Logradouro =
            request.Logradouro?.Trim()
            ?? string.Empty;

        empresa.Numero =
            request.Numero?.Trim()
            ?? string.Empty;

        empresa.Bairro =
            request.Bairro?.Trim()
            ?? string.Empty;

        empresa.CodigoMunicipio =
            request.CodigoMunicipio;

        empresa.Municipio =
            request.Municipio?.Trim()
            ?? string.Empty;

        empresa.Uf =
            request.Uf?.Trim().ToUpper()
            ?? string.Empty;

        empresa.Cep = new string(
            (request.Cep ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray()
        );

        empresa.Crt = request.Crt;

        await _context.SaveChangesAsync();

        /* --------------------------------------------------------
           RETORNO
           -------------------------------------------------------- */

        return Ok(new
        {
            empresa.Id,
            empresa.Cnpj,
            empresa.RazaoSocial,
            empresa.NomeFantasia,
            empresa.InscricaoEstadual,

            empresa.Logradouro,
            empresa.Numero,
            empresa.Bairro,

            empresa.CodigoMunicipio,
            empresa.Municipio,
            empresa.Uf,
            empresa.Cep,

            empresa.Crt,
            empresa.Ativo
        });
    }
}