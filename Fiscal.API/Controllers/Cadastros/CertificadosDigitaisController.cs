using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros;

[ApiController]
[Authorize]
[Route("api/cadastros/certificados-digitais")]
public class CertificadosDigitaisController : ControllerBase
{
    private readonly FiscalDbContext _context;

    public CertificadosDigitaisController(
        FiscalDbContext context)
    {
        _context = context;
    }

    [HttpGet("meu")]
    public async Task<IActionResult> ObterMeuCertificado()
    {
        if (!TryObterEmpresaId(out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var certificado =
            await _context.CertificadosDigitais
                .AsNoTracking()
                .Where(x =>
                    x.EmpresaId == empresaId &&
                    x.Ativo)
                .Select(x => new
                {
                    x.Id,
                    x.TipoArmazenamento,
                    x.Titular,
                    x.Cnpj,
                    x.Emissor,
                    x.Thumbprint,
                    x.ValidoDe,
                    x.ValidoAte,
                    x.Ativo,
                    x.AtualizadoEm
                })
                .FirstOrDefaultAsync();

        if (certificado is null)
        {
            return NotFound(new
            {
                mensagem = "Certificado digital não configurado."
            });
        }

        return Ok(certificado);
    }

    [HttpPut("meu")]
    public async Task<IActionResult> AtualizarMeuCertificado(
        [FromBody] AtualizarCertificadoDigitalRequest request)
    {
        if (!TryObterEmpresaId(out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Thumbprint))
        {
            return BadRequest(new
            {
                mensagem = "Thumbprint do certificado não informado."
            });
        }

        if (request.ValidoAte.HasValue &&
            request.ValidoDe.HasValue &&
            request.ValidoAte.Value <= request.ValidoDe.Value)
        {
            return BadRequest(new
            {
                mensagem = "A validade final do certificado é inválida."
            });
        }

        var certificado =
            await _context.CertificadosDigitais
                .FirstOrDefaultAsync(
                    x => x.EmpresaId == empresaId);

        if (certificado is null)
        {
            certificado = new CertificadoDigital
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                CriadoEm = DateTime.UtcNow
            };

            _context.CertificadosDigitais.Add(certificado);
        }

        certificado.TipoArmazenamento =
            TipoArmazenamentoCertificado.Local;

        certificado.Titular =
            Normalizar(request.Titular);

        certificado.Cnpj =
            SomenteNumeros(request.Cnpj);

        certificado.Emissor =
            Normalizar(request.Emissor);

        certificado.Thumbprint =
            request.Thumbprint.Trim();

        certificado.ValidoDe =
            request.ValidoDe;

        certificado.ValidoAte =
            request.ValidoAte;

        certificado.Ativo = true;

        certificado.AtualizadoEm =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            certificado.Id,
            certificado.TipoArmazenamento,
            certificado.Titular,
            certificado.Cnpj,
            certificado.Emissor,
            certificado.Thumbprint,
            certificado.ValidoDe,
            certificado.ValidoAte,
            certificado.Ativo,
            certificado.AtualizadoEm
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

    private static string? Normalizar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }

    private static string? SomenteNumeros(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        return new string(
            valor.Where(char.IsDigit).ToArray()
        );
    }
}

public class AtualizarCertificadoDigitalRequest
{
    public string? Titular { get; set; }

    public string? Cnpj { get; set; }

    public string? Emissor { get; set; }

    public string? Thumbprint { get; set; }

    public DateTime? ValidoDe { get; set; }

    public DateTime? ValidoAte { get; set; }
}