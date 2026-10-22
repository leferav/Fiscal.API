using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Requests.Cadastros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace Fiscal.API.Controllers.Cadastros
{
    [ApiController]
    [Authorize]
    [Route("api/cadastros/produtos")]
    public class ProdutosController : ControllerBase
    {
        private readonly FiscalDbContext _context;

        public ProdutosController(FiscalDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObterProdutos()
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

            var produtos = await _context.Produtos
                .Where(x =>
                    x.EmpresaId == empresaId &&
                    x.Ativo)
                .OrderBy(x => x.Descricao)
                .Select(x => new
                {
                    x.Id,
                    x.Codigo,
                    x.Descricao,
                    x.Ncm,
                    x.Unidade,
                    x.ValorVenda,
                    x.ConfiguracaoTributariaId,
                    x.Ativo
                })
                .ToListAsync();

            return Ok(produtos);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
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

            var produto = await _context.Produtos
                .AsNoTracking()
                .Where(x =>
                    x.Id == id &&
                    x.EmpresaId == empresaId)
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
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (string.IsNullOrWhiteSpace(empresaIdClaim) ||
                !Guid.TryParse(empresaIdClaim, out var empresaId))
            {
                return Unauthorized(new
                {
                    mensagem = "Empresa não identificada no token."
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
                        x.Id == empresaId &&
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
                        x.EmpresaId == empresaId &&
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
                        x.EmpresaId == empresaId &&
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

                EmpresaId = empresaId,

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

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Alterar(Guid id, [FromBody] AlterarProdutoRequest request)
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

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.EmpresaId == empresaId);

            if (produto == null)
            {
                return NotFound(new
                {
                    erro = "Produto não encontrado."
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

            if (request.ValorVenda <= 0)
            {
                return BadRequest(new
                {
                    erro = "O valor de venda deve ser maior que zero."
                });
            }

            var configuracaoExiste =
                await _context.ConfiguracoesTributarias.AnyAsync(x =>
                    x.Id == request.ConfiguracaoTributariaId &&
                    x.EmpresaId == empresaId &&
                    x.Ativo);

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
                await _context.Produtos.AnyAsync(x =>
                    x.EmpresaId == empresaId &&
                    x.Codigo == codigo &&
                    x.Id != id);

            if (codigoExiste)
            {
                return BadRequest(new
                {
                    erro =
                        $"Já existe outro produto com o código {codigo} nesta empresa."
                });
            }

            produto.Codigo = codigo;
            produto.Descricao = request.Descricao.Trim();
            produto.Ncm = request.Ncm.Trim();

            produto.Unidade =
                string.IsNullOrWhiteSpace(request.Unidade)
                    ? "UN"
                    : request.Unidade.Trim().ToUpper();

            produto.ValorVenda = request.ValorVenda;

            produto.ConfiguracaoTributariaId =
                request.ConfiguracaoTributariaId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                produto.Id,
                produto.EmpresaId,
                produto.Codigo,
                produto.Descricao,
                produto.Ncm,
                produto.Unidade,
                produto.ValorVenda,
                produto.ConfiguracaoTributariaId,
                produto.Ativo
            });
        }


        [HttpPost("{id:guid}/imagem")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> EnviarImagem( Guid id, IFormFile arquivo)
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (!Guid.TryParse(empresaIdClaim, out var empresaId))
                return Unauthorized(new { mensagem = "Empresa não identificada no token." });

            var produto = await _context.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId);

            if (produto == null)
                return NotFound(new { mensagem = "Produto não encontrado." });

            if (arquivo == null || arquivo.Length == 0)
                return BadRequest(new { mensagem = "Selecione uma imagem." });

            if (arquivo.Length > 1024 * 1024)
                return BadRequest(new { mensagem = "A imagem deve ter no máximo 1 MB." });

            var formatos = new[] { "image/jpeg", "image/png", "image/webp" };

            if (!formatos.Contains(arquivo.ContentType.ToLowerInvariant()))
                return BadRequest(new { mensagem = "Formato inválido. Utilize JPG, PNG ou WebP." });




            byte[] imagemBytes;


            try
            {
                using var stream = arquivo.OpenReadStream();
                using var memoria = new MemoryStream();

                await stream.CopyToAsync(memoria);

                using var imagemOriginal = SKBitmap.Decode(memoria.ToArray());

                if (imagemOriginal == null)
                {
                    return BadRequest(new
                    {
                        mensagem = "O arquivo não contém uma imagem válida."
                    });
                }

                const int tamanhoMaximo = 600;

                double escala = Math.Min(
                    1.0,
                    Math.Min(
                        (double)tamanhoMaximo / imagemOriginal.Width,
                        (double)tamanhoMaximo / imagemOriginal.Height
                    )
                );

                int largura = Math.Max(
                    1,
                    (int)Math.Round(imagemOriginal.Width * escala)
                );

                int altura = Math.Max(
                    1,
                    (int)Math.Round(imagemOriginal.Height * escala)
                );

                using var imagemRedimensionada = new SKBitmap(
                    largura,
                    altura
                );

                using (var canvas = new SKCanvas(imagemRedimensionada))
                {
                    canvas.Clear(SKColors.Transparent);

                    using var paint = new SKPaint
                    {
                        IsAntialias = true
                    };

                    canvas.DrawBitmap(
                        imagemOriginal,
                        new SKRect(0, 0, largura, altura),
                        paint
                    );

                    canvas.Flush();
                }

                using var imagemFinal = SKImage.FromBitmap(imagemRedimensionada);

                using var dadosWebp = imagemFinal.Encode(
                    SKEncodedImageFormat.Webp,
                    80
                );

                if (dadosWebp == null)
                {
                    return BadRequest(new
                    {
                        mensagem = "Não foi possível converter a imagem para WebP."
                    });
                }

                imagemBytes = dadosWebp.ToArray();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                return BadRequest(new
                {
                    mensagem = "Não foi possível processar a imagem enviada."
                });
            }


            var imagemProduto = await _context.ProdutosImagens
                .FirstOrDefaultAsync(x => x.ProdutoId == id);

            if (imagemProduto == null)
            {
                imagemProduto = new ProdutoImagem
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = id,
                    Imagem = imagemBytes,
                    ContentType = "image/webp",
                    TamanhoBytes = imagemBytes.Length,
                    CriadoEm = DateTime.UtcNow
                };

                _context.ProdutosImagens.Add(imagemProduto);
            }
            else
            {
                imagemProduto.Imagem = imagemBytes;
                imagemProduto.ContentType = "image/webp";
                imagemProduto.TamanhoBytes = imagemBytes.Length;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Imagem do produto salva com sucesso.",
                produtoId = id,
                tamanhoBytes = imagemBytes.Length
            });
        }


        [HttpGet("{id:guid}/imagem")]
        public async Task<IActionResult> ObterImagem(Guid id)
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (!Guid.TryParse(empresaIdClaim, out var empresaId))
            {
                return Unauthorized(new
                {
                    mensagem = "Empresa não identificada no token."
                });
            }

            var imagem = await _context.ProdutosImagens
                .AsNoTracking()
                .Where(x =>
                    x.ProdutoId == id &&
                    x.Produto.EmpresaId == empresaId)
                .Select(x => new
                {
                    x.Imagem,
                    x.ContentType
                })
                .FirstOrDefaultAsync();

            if (imagem == null)
            {
                return NotFound(new
                {
                    mensagem = "Este produto não possui imagem cadastrada."
                });
            }

            return File(imagem.Imagem, imagem.ContentType);
        }


    }
}