using Fiscal.API.Data;
using Fiscal.API.DTOs.NotasFiscais;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using System.Security.Claims;

namespace Fiscal.API.Controllers;

[ApiController]
[Route("api/notas-fiscais")]
[Authorize]
public class NotasFiscaisController : ControllerBase
{
    private readonly FiscalDbContext _context;

    public NotasFiscaisController(FiscalDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var empresaIdClaim = User.FindFirstValue("empresaId");

        if (!Guid.TryParse(empresaIdClaim, out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var notas = await _context.NotasFiscais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderByDescending(x => x.CriadoEm)
            .Select(x => new NotaFiscalListaDto
            {
                Id = x.Id,
                Modelo = x.Modelo,
                Serie = x.Serie,
                Numero = x.Numero,
                ChaveAcesso = x.ChaveAcesso,
                Ambiente = x.Ambiente,
                Status = x.Status,
                CStat = x.CStat,
                XMotivo = x.XMotivo,
                ValorProdutos = x.ValorProdutos,
                ValorTotal = x.ValorTotal,
                CriadoEm = x.CriadoEm,
                AutorizadoEm = x.AutorizadoEm
            })
            .ToListAsync();

        return Ok(notas);
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var empresaIdClaim = User.FindFirstValue("empresaId");

        if (!Guid.TryParse(empresaIdClaim, out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var nota = await _context.NotasFiscais
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.EmpresaId == empresaId)
            .Select(x => new NotaFiscalDetalhesDto
            {
                Id = x.Id,
                Modelo = x.Modelo,
                Serie = x.Serie,
                Numero = x.Numero,
                ChaveAcesso = x.ChaveAcesso,
                Ambiente = x.Ambiente,
                Status = x.Status,
                Protocolo = x.Protocolo,
                Recibo = x.Recibo,
                CStat = x.CStat,
                XMotivo = x.XMotivo,
                ValorProdutos = x.ValorProdutos,
                ValorTotal = x.ValorTotal,
                CriadoEm = x.CriadoEm,
                AtualizadoEm = x.AtualizadoEm,
                AutorizadoEm = x.AutorizadoEm
            })
            .FirstOrDefaultAsync();

        if (nota == null)
        {
            return NotFound(new
            {
                mensagem = "Nota fiscal não encontrada."
            });
        }

        return Ok(nota);
    }

    [HttpGet("{id:guid}/xml")]
    public async Task<IActionResult> ObterXml(Guid id)
    {
        var empresaIdClaim = User.FindFirstValue("empresaId");

        if (!Guid.TryParse(empresaIdClaim, out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var nota = await _context.NotasFiscais
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.EmpresaId == empresaId)
            .Select(x => new
            {
                x.Numero,
                x.Modelo,
                x.XmlAutorizado
            })
            .FirstOrDefaultAsync();

        if (nota == null)
        {
            return NotFound(new
            {
                mensagem = "Nota fiscal não encontrada."
            });
        }

        if (string.IsNullOrWhiteSpace(nota.XmlAutorizado))
        {
            return NotFound(new
            {
                mensagem = "XML autorizado não disponível para esta nota fiscal."
            });
        }

        var nomeArquivo =
            nota.Modelo == 65
                ? $"NFCe-{nota.Numero}.xml"
                : $"NFe-{nota.Numero}.xml";

        return File(
            System.Text.Encoding.UTF8.GetBytes(nota.XmlAutorizado),
            "application/xml",
            nomeArquivo
        );
    }


    [HttpGet("{id:guid}/danfe")]
    public async Task<IActionResult> ObterDanfe(Guid id)
    {
        var empresaIdClaim = User.FindFirstValue("empresaId");

        if (!Guid.TryParse(empresaIdClaim, out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var nota = await _context.NotasFiscais
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.EmpresaId == empresaId)
            .Select(x => new
            {
                x.Numero,
                x.Modelo,
                x.XmlAutorizado
            })
            .FirstOrDefaultAsync();

        if (nota == null)
        {
            return NotFound(new
            {
                mensagem = "Nota fiscal não encontrada."
            });
        }

        if (string.IsNullOrWhiteSpace(nota.XmlAutorizado))
        {
            return NotFound(new
            {
                mensagem = "XML autorizado não disponível para esta nota fiscal."
            });
        }

        if (nota.Modelo != 65)
        {
            return BadRequest(new
            {
                mensagem = "Geração de DANFE disponível apenas para NFC-e."
            });
        }

        var documento =
            new NFe.Danfe.QuestPdf.ImpressaoNfce.DanfeNfceDocument(
                nota.XmlAutorizado,
                null
            );

        documento.TamanhoImpressao(
            NFe.Danfe.QuestPdf.ImpressaoNfce.TamanhoImpressao.Impressao80
        );

        var pdf = documento.GeneratePdf();

        return File(
            pdf,
            "application/pdf",
            $"NFCe-{nota.Numero}.pdf"
        );

    }
}