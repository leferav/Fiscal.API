using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Requests.Cadastros;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros
{
    [ApiController]
    [Route("api/cadastros/configuracoes-tributarias")]
    public class ConfiguracoesTributariasController : ControllerBase
    {
        private readonly FiscalDbContext _context;

        public ConfiguracoesTributariasController(
            FiscalDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET - configurações tributárias da empresa
        // ============================================================
        [HttpGet("empresa/{empresaId:guid}")]
        public async Task<IActionResult> ObterPorEmpresa(
            Guid empresaId)
        {
            var configuracoes =
                await _context.ConfiguracoesTributarias
                    .AsNoTracking()
                    .Where(x =>
                        x.EmpresaId == empresaId &&
                        x.Ativo)
                    .OrderBy(x => x.Nome)
                    .ToListAsync();

            return Ok(configuracoes);
        }

        // ============================================================
        // POST - cadastrar configuração tributária
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Cadastrar(
            [FromBody]
            CadastrarConfiguracaoTributariaRequest request)
        {
            var empresaExiste =
                await _context.Empresas
                    .AnyAsync(x =>
                        x.Id == request.EmpresaId);

            if (!empresaExiste)
            {
                return BadRequest(new
                {
                    erro = "Empresa não encontrada."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Nome))
            {
                return BadRequest(new
                {
                    erro = "Informe o nome da configuração tributária."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Cfop))
            {
                return BadRequest(new
                {
                    erro = "Informe o CFOP."
                });
            }

            var configuracao =
                new ConfiguracaoTributaria
                {
                    Id = Guid.NewGuid(),

                    EmpresaId = request.EmpresaId,

                    Nome = request.Nome.Trim(),

                    CstIcms =
                        request.CstIcms?.Trim(),

                    Csosn =
                        request.Csosn?.Trim(),

                    AliquotaIcms =
                        request.AliquotaIcms,

                    Cfop =
                        request.Cfop.Trim(),

                    Ativo = true,

                    CriadoEm = DateTime.UtcNow
                };

            _context.ConfiguracoesTributarias
                .Add(configuracao);

            await _context.SaveChangesAsync();

            return Ok(configuracao);
        }
    }
}