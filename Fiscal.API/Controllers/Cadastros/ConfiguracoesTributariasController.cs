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

            var erroValidacao =
                ValidarConfiguracaoTributaria(request);

            if (erroValidacao is not null)
            {
                return erroValidacao;
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

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] CadastrarConfiguracaoTributariaRequest request)
        {
            var empresaIdClaim =
                User.FindFirst("empresaId")?.Value;

            if (string.IsNullOrWhiteSpace(empresaIdClaim) ||
                !Guid.TryParse(empresaIdClaim, out var empresaId))
            {
                return Unauthorized(new
                {
                    mensagem = "Empresa não identificada no token."
                });
            }

            var configuracao =
                await _context.ConfiguracoesTributarias
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.EmpresaId == empresaId &&
                        x.Ativo);

            if (configuracao is null)
            {
                return NotFound(new
                {
                    mensagem =
                        "Configuração tributária não encontrada."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Nome))
            {
                return BadRequest(new
                {
                    mensagem =
                        "Informe o nome da configuração tributária."
                });
            }

            var erroValidacao =
                ValidarConfiguracaoTributaria(request);

            if (erroValidacao is not null)  
            {
                return erroValidacao;
            }

            configuracao.Nome =
                request.Nome.Trim();

            configuracao.Cfop =
                request.Cfop.Trim();

            configuracao.CstIcms =
                request.CstIcms?.Trim();

            configuracao.Csosn =
                request.Csosn?.Trim();

            configuracao.AliquotaIcms =
                request.AliquotaIcms;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                configuracao.Id,
                configuracao.Nome,
                configuracao.Cfop,
                configuracao.CstIcms,
                configuracao.Csosn,
                configuracao.AliquotaIcms,
                configuracao.Ativo
            });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var empresaIdClaim =
                User.FindFirst("empresaId")?.Value;

            if (string.IsNullOrWhiteSpace(empresaIdClaim) ||
                !Guid.TryParse(empresaIdClaim, out var empresaId))
            {
                return Unauthorized(new
                {
                    mensagem = "Empresa não identificada no token."
                });
            }

            var configuracao =
                await _context.ConfiguracoesTributarias
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.EmpresaId == empresaId &&
                        x.Ativo);

            if (configuracao is null)
            {
                return NotFound(new
                {
                    mensagem =
                        "Configuração tributária não encontrada."
                });
            }

            var utilizadaPorProduto = await _context.Produtos.AnyAsync(x =>
            x.EmpresaId == empresaId &&
            x.ConfiguracaoTributariaId == id &&
            x.Ativo);

            if (utilizadaPorProduto)
            {
                return BadRequest(new
                {
                    mensagem =
                        "Esta configuração tributária está sendo utilizada por produtos ativos e não pode ser desativada."
                });
            }

            configuracao.Ativo = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem =
                    "Configuração tributária desativada com sucesso."
            });
        }


        private IActionResult? ValidarConfiguracaoTributaria(CadastrarConfiguracaoTributariaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nome))
            {
                return BadRequest(new
                {
                    mensagem = "Informe o nome da configuração tributária."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Cfop) ||
                request.Cfop.Trim().Length != 4 ||
                !request.Cfop.Trim().All(char.IsDigit))
            {
                return BadRequest(new
                {
                    mensagem = "O CFOP deve possuir exatamente 4 números."
                });
            }

            if (!string.IsNullOrWhiteSpace(request.CstIcms))
            {
                var cst = request.CstIcms.Trim();

                if (cst.Length != 2 || !cst.All(char.IsDigit))
                {
                    return BadRequest(new
                    {
                        mensagem =
                            "O CST ICMS deve possuir exatamente 2 números."
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Csosn))
            {
                var csosn = request.Csosn.Trim();

                if (csosn.Length != 3 || !csosn.All(char.IsDigit))
                {
                    return BadRequest(new
                    {
                        mensagem =
                            "O CSOSN deve possuir exatamente 3 números."
                    });
                }
            }

            if (request.AliquotaIcms < 0 ||
                request.AliquotaIcms > 100)
            {
                return BadRequest(new
                {
                    mensagem =
                        "A alíquota ICMS deve estar entre 0 e 100."
                });
            }

            return null;
        }
    }

}