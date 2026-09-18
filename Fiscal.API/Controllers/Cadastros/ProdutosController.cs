using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Requests.Cadastros;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros
{
    [ApiController]
    [Route("api/cadastros/produtos")]
    public class ProdutosController : ControllerBase
    {
        private readonly FiscalDbContext _context;

        public ProdutosController(FiscalDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var produto = await _context.Produtos
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.EmpresaId,
                    x.Codigo,
                    x.Descricao,
                    x.Ncm,
                    x.Unidade,
                    x.ConfiguracaoTributariaId,

                    Tributacao = new
                    {
                        x.ConfiguracaoTributaria.Nome,
                        x.ConfiguracaoTributaria.Cfop,
                        x.ConfiguracaoTributaria.CstIcms,
                        x.ConfiguracaoTributaria.Csosn,
                        x.ConfiguracaoTributaria.AliquotaIcms
                    },

                    x.Ativo
                })
                .FirstOrDefaultAsync();

            if (produto == null)
            {
                return NotFound(new
                {
                    erro = "Produto não encontrado."
                });
            }

            return Ok(produto);
        }

        [HttpGet("empresa/{empresaId:guid}")]
        public async Task<IActionResult> ObterPorEmpresa(Guid empresaId)
        {
            var produtos = await _context.Produtos
                .AsNoTracking()
                .Where(x =>
                    x.EmpresaId == empresaId &&
                    x.Ativo)
                .OrderBy(x => x.Descricao)
                .Select(x => new
                {
                    x.Id,
                    x.EmpresaId,
                    x.Codigo,
                    x.Descricao,
                    x.Ncm,
                    x.Unidade,
                    x.ValorVenda,
                    x.ConfiguracaoTributariaId,

                    Tributacao = new
                    {
                        x.ConfiguracaoTributaria.Nome,
                        x.ConfiguracaoTributaria.Cfop,
                        x.ConfiguracaoTributaria.CstIcms,
                        x.ConfiguracaoTributaria.Csosn,
                        x.ConfiguracaoTributaria.AliquotaIcms
                    },

                    x.Ativo
                })
                .ToListAsync();

            return Ok(produtos);
        }

        [HttpPost]
        public async Task<IActionResult> Cadastrar([FromBody] CadastrarProdutoRequest request)
        {
            if (request.EmpresaId == Guid.Empty)
            {
                return BadRequest(new
                {
                    erro = "Informe a empresa."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Codigo))
            {
                return BadRequest(new
                {
                    erro = "Informe o código do produto."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Descricao))
            {
                return BadRequest(new
                {
                    erro = "Informe a descrição do produto."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Ncm))
            {
                return BadRequest(new
                {
                    erro = "Informe o NCM do produto."
                });
            }

            if (request.ConfiguracaoTributariaId == Guid.Empty)
            {
                return BadRequest(new
                {
                    erro = "Informe a configuração tributária."
                });
            }

            var empresaExiste =
                await _context.Empresas.AnyAsync(
                    x =>
                        x.Id == request.EmpresaId &&
                        x.Ativo
                );

            if (!empresaExiste)
            {
                return BadRequest(new
                {
                    erro = "Empresa não encontrada ou inativa."
                });
            }

            // A configuração precisa pertencer à mesma empresa.
            var configuracaoExiste =
                await _context.ConfiguracoesTributarias.AnyAsync(
                    x =>
                        x.Id == request.ConfiguracaoTributariaId &&
                        x.EmpresaId == request.EmpresaId &&
                        x.Ativo
                );

            if (!configuracaoExiste)
            {
                return BadRequest(new
                {
                    erro =
                        "Configuração tributária não encontrada, inativa " +
                        "ou pertencente a outra empresa."
                });
            }

            var codigo = request.Codigo.Trim();

            var codigoExiste =
                await _context.Produtos.AnyAsync(
                    x =>
                        x.EmpresaId == request.EmpresaId &&
                        x.Codigo == codigo
                );

            if (codigoExiste)
            {
                return BadRequest(new
                {
                    erro =
                        $"Já existe um produto com o código {codigo} nesta empresa."
                });
            }

            if (request.ValorVenda <= 0)
            {
                return BadRequest("O valor de venda deve ser maior que zero.");
            }

            var produto = new Produto
            {
                Id = Guid.NewGuid(),

                EmpresaId = request.EmpresaId,

                Codigo = codigo,

                Descricao = request.Descricao.Trim(),

                Ncm = request.Ncm.Trim(),

                Unidade =
                    string.IsNullOrWhiteSpace(request.Unidade)
                        ? "UN"
                        : request.Unidade.Trim().ToUpper(),

                ConfiguracaoTributariaId =
                    request.ConfiguracaoTributariaId,

                ValorVenda = request.ValorVenda,

                Ativo = true,

                CriadoEm = DateTime.UtcNow
            };

            _context.Produtos.Add(produto);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                produto.Id,
                produto.EmpresaId,
                produto.Codigo,
                produto.Descricao,
                produto.Ncm,
                produto.Unidade,
                produto.ConfiguracaoTributariaId,
                produto.ValorVenda,
                produto.Ativo
            });
        }
    }
}