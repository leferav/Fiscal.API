using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Requests.Cadastros;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros;

[ApiController]
[Route("api/cadastros/configuracoes-fiscais")]
public class ConfiguracoesFiscaisController : ControllerBase
{
    private readonly FiscalDbContext _context;

    public ConfiguracoesFiscaisController(FiscalDbContext context)
    {
        _context = context;
    }

    [HttpGet("{empresaId:guid}")]
    public async Task<ActionResult<ConfiguracaoFiscal>> GetByEmpresa(Guid empresaId)
    {
        var configuracao = await _context.ConfiguracoesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId);

        if (configuracao is null)
            return NotFound();

        return Ok(configuracao);
    }

    [HttpPost]
    public async Task<ActionResult<ConfiguracaoFiscal>> Post(CadastrarConfiguracaoFiscalRequest request)
    {
        var empresaExiste = await _context.Empresas
            .AnyAsync(x => x.Id == request.EmpresaId);

        if (!empresaExiste)
            return BadRequest("Empresa não encontrada.");

        var configuracaoExiste = await _context.ConfiguracoesFiscais
            .AnyAsync(x => x.EmpresaId == request.EmpresaId);

        if (configuracaoExiste)
            return Conflict(
                "A empresa já possui uma configuração fiscal.");

        var configuracao = new ConfiguracaoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = request.EmpresaId,
            Ambiente = request.Ambiente,
            SerieNFe = request.SerieNFe,
            SerieNFCe = request.SerieNFCe,
            ProximoNumeroNFe = request.ProximoNumeroNFe,
            ProximoNumeroNFCe = request.ProximoNumeroNFCe,
            Csc = request.Csc,
            CscId = request.CscId,
            CriadoEm = DateTime.UtcNow
        };

        _context.ConfiguracoesFiscais.Add(configuracao);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetByEmpresa),
            new { empresaId = configuracao.EmpresaId },
            configuracao);
    }
}