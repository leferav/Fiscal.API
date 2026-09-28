using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Requests.Cadastros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros
{
    [ApiController]
    [Authorize]
    [Route("api/cadastros/configuracoes-tributarias")]
    public class ConfiguracoesTributariasController : ControllerBase
    {
        private readonly FiscalDbContext _context;

        public ConfiguracoesTributariasController(
            FiscalDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObterConfiguracoes()
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (string.IsNullOrWhiteSpace(empresaIdClaim) ||
                !Guid.TryParse(empresaIdClaim, out var empresaId))
            {
                return Unauthorized(new
                {
                    mensagem = "Empresa não identificada no token."
                });
            }

            var configuracoes =
                await _context.ConfiguracoesTributarias
                    .AsNoTracking()
                    .Where(x =>
                        x.EmpresaId == empresaId &&
                        x.Ativo)
                    .OrderBy(x => x.Nome)
                    .Select(x => new
                    {
                        x.Id,
                        x.Nome,
                        x.Cfop,
                        x.CstIcms,
                        x.Csosn,
                        x.AliquotaIcms,
                        x.Ativo
                    })
                    .ToListAsync();

            return Ok(configuracoes);
        }


        [HttpPost]
        public async Task<IActionResult> Cadastrar([FromBody] CadastrarConfiguracaoTributariaRequest request)
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (string.IsNullOrWhiteSpace(empresaIdClaim) ||
                !Guid.TryParse(empresaIdClaim, out var empresaId))
            {
                return Unauthorized(new
                {
                    mensagem = "Empresa não identificada no token."
                });
            }

            var empresaExiste =
                await _context.Empresas
                    .AnyAsync(x =>
                        x.Id == empresaId &&
                        x.Ativo);

            if (!empresaExiste)
            {
                return BadRequest(new
                {
                    erro = "Empresa não encontrada ou inativa."
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

                    EmpresaId = empresaId,

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