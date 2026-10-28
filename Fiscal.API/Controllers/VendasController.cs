
using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.NFe;
using Fiscal.API.Services;
using Fiscal.API.Services.NFe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers;

[ApiController]
[Authorize]
[Route("api/vendas")]
public class VendasController : ControllerBase
{
    private readonly FiscalDbContext _context;
    private readonly FiscalService _fiscalService;

    public VendasController(
        FiscalDbContext context,
        FiscalService fiscalService)
    {
        _context = context;
        _fiscalService = fiscalService;
    }

    [HttpPost]
    [Authorize(Roles = "Master,Administrador,Operador")]
    public async Task<IActionResult> Criar(
        [FromBody] CriarVendaRequest request)
    {
        if (!Guid.TryParse(
            User.FindFirst("empresaId")?.Value,
            out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        if (request == null ||
            request.Produtos == null ||
            request.Produtos.Count == 0)
        {
            return BadRequest(new
            {
                mensagem = "Informe pelo menos um produto."
            });
        }

        if (request.FormaPagamento is not
            ("01" or "03" or "04" or "17"))
        {
            return BadRequest(new
            {
                mensagem = "Forma de pagamento inválida."
            });
        }

        var cpf = string.IsNullOrWhiteSpace(request.CpfConsumidor)
            ? null
            : new string(
                request.CpfConsumidor.Where(char.IsDigit).ToArray());

        if (cpf != null && cpf.Length != 11)
        {
            return BadRequest(new
            {
                mensagem = "O CPF deve conter 11 dígitos."
            });
        }

        if (request.Produtos.Any(x =>
            x.ProdutoId == Guid.Empty ||
            x.Quantidade <= 0))
        {
            return BadRequest(new
            {
                mensagem = "Produto ou quantidade inválida."
            });
        }

        var empresaAtiva = await _context.Empresas
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == empresaId &&
                x.Ativo);

        if (!empresaAtiva)
        {
            return UnprocessableEntity(new
            {
                mensagem = "Empresa não encontrada ou inativa."
            });
        }

        var ids = request.Produtos
            .Select(x => x.ProdutoId)
            .Distinct()
            .ToList();

        var produtos = await _context.Produtos
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.Ativo &&
                ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        if (produtos.Count != ids.Count)
        {
            return BadRequest(new
            {
                mensagem = "Há produtos inexistentes, inativos ou de outra empresa."
            });
        }

        var venda = new Venda
        {
            EmpresaId = empresaId,
            Status = "A_ENVIAR",
            FormaPagamento = request.FormaPagamento,
            CpfConsumidor = cpf,
            CriadoEm = DateTime.UtcNow
        };

        foreach (var item in request.Produtos)
        {
            var produto = produtos[item.ProdutoId];

            if (produto.ValorVenda <= 0)
            {
                return BadRequest(new
                {
                    mensagem = $"O produto {produto.Codigo} não possui preço válido."
                });
            }

            var totalItem = decimal.Round(
                item.Quantidade * produto.ValorVenda,
                2,
                MidpointRounding.AwayFromZero);

            venda.Itens.Add(new VendaItem
            {
                ProdutoId = produto.Id,
                CodigoProduto = produto.Codigo,
                DescricaoProduto = produto.Descricao,
                Quantidade = item.Quantidade,
                ValorUnitario = produto.ValorVenda,
                ValorTotal = totalItem
            });
        }

        venda.ValorTotal = venda.Itens.Sum(x => x.ValorTotal);

        _context.Vendas.Add(venda);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            venda.Id,
            venda.EmpresaId,
            venda.Status,
            venda.FormaPagamento,
            venda.CpfConsumidor,
            venda.ValorTotal,
            venda.CriadoEm,
            quantidadeItens = venda.Itens.Count
        });
    }

    [HttpGet("pendentes")]
    [Authorize(Roles = "Master,Administrador,Operador,Consulta")]
    public async Task<IActionResult> ConsultarPendentes()
    {
        if (!Guid.TryParse(
            User.FindFirst("empresaId")?.Value,
            out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var vendas = await _context.Vendas
            .AsNoTracking()
            .Where(v =>
                v.EmpresaId == empresaId &&
                v.Status == "A_ENVIAR")
            .OrderByDescending(v => v.CriadoEm)
            .Select(v => new
            {
                v.Id,
                v.EmpresaId,
                v.Status,
                v.FormaPagamento,
                v.CpfConsumidor,
                v.ValorTotal,
                v.CriadoEm,
                quantidadeItens = v.Itens.Count,
                itens = v.Itens.Select(i => new
                {
                    i.Id,
                    i.ProdutoId,
                    i.CodigoProduto,
                    i.DescricaoProduto,
                    i.Quantidade,
                    i.ValorUnitario,
                    i.ValorTotal
                }).ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            quantidade = vendas.Count,
            vendas
        });
    }

    [HttpPost("{id:guid}/emitir")]
    [Authorize(Roles = "Master,Administrador,Operador")]
    public async Task<IActionResult> Emitir(Guid id)
    {
        if (!Guid.TryParse(
            User.FindFirst("empresaId")?.Value,
            out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        // Reserva atômica da venda.
        var agora = DateTime.UtcNow;

        var linhasAfetadas = await _context.Vendas
            .Where(v =>
                v.Id == id &&
                v.EmpresaId == empresaId &&
                v.Status == "A_ENVIAR")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(v => v.Status, "EMITINDO")
                .SetProperty(v => v.AtualizadoEm, agora));

        if (linhasAfetadas == 0)
        {
            return Conflict(new
            {
                mensagem = "Venda inexistente, já reservada ou não disponível para emissão."
            });
        }

        try
        {
            var venda = await _context.Vendas
                .AsNoTracking()
                .Include(v => v.Itens)
                .FirstAsync(v =>
                    v.Id == id &&
                    v.EmpresaId == empresaId);

            var ids = venda.Itens
                .Select(i => i.ProdutoId)
                .Distinct()
                .ToList();

            var produtos = await _context.Produtos
                .AsNoTracking()
                .Include(p => p.ConfiguracaoTributaria)
                .Where(p =>
                    p.EmpresaId == empresaId &&
                    p.Ativo &&
                    ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var itensFiscais = new List<ItemFiscal>();

            foreach (var item in venda.Itens)
            {
                if (!produtos.TryGetValue(
                        item.ProdutoId,
                        out var produto) ||
                    produto.ConfiguracaoTributaria == null ||
                    !produto.ConfiguracaoTributaria.Ativo)
                {
                    throw new InvalidOperationException(
                        $"Produto {item.CodigoProduto} indisponível para emissão fiscal.");
                }

                itensFiscais.Add(new ItemFiscal
                {
                    ProdutoId = produto.Id,
                    Codigo = item.CodigoProduto,
                    Descricao = item.DescricaoProduto,
                    Ncm = produto.Ncm,
                    Unidade = produto.Unidade,
                    Quantidade = item.Quantidade,
                    ValorUnitario = item.ValorUnitario,
                    ConfiguracaoTributaria =
                        produto.ConfiguracaoTributaria
                });
            }

            var request = new EmitirNFeRequest
            {
                EmpresaId = empresaId
            };

            if (!string.IsNullOrWhiteSpace(venda.CpfConsumidor))
            {
                request.Destinatario = new DestinatarioRequest
                {
                    CpfCnpj = venda.CpfConsumidor
                };
            }

            var resultado = await _fiscalService.AutorizarNFCeAsync(
                request,
                itensFiscais,
                venda.FormaPagamento,
                venda.Id);

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            // Não reenviar automaticamente uma venda
            // que já possui tentativa fiscal registrada.

            var notaId = await _context.Vendas
                .AsNoTracking()
                .Where(v =>
                    v.Id == id &&
                    v.EmpresaId == empresaId)
                .Select(v => v.NotaFiscalId)
                .FirstOrDefaultAsync();

            if (notaId == null)
            {
                await _context.Vendas
                    .Where(v =>
                        v.Id == id &&
                        v.EmpresaId == empresaId &&
                        v.Status == "EMITINDO" &&
                        v.NotaFiscalId == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(v => v.Status, "A_ENVIAR")
                        .SetProperty(v =>
                            v.AtualizadoEm,
                            DateTime.UtcNow));

                return UnprocessableEntity(new
                {
                    mensagem = ex.Message
                });
            }

            await _context.Vendas
                .Where(v =>
                    v.Id == id &&
                    v.EmpresaId == empresaId &&
                    v.NotaFiscalId == notaId &&
                    v.Status == "EMITINDO")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.Status, "VERIFICAR_SEFAZ")
                    .SetProperty(v =>
                        v.AtualizadoEm,
                        DateTime.UtcNow));

            return StatusCode(409, new
            {
                mensagem =
                    "Existe uma tentativa fiscal registrada. Consulte a SEFAZ antes de nova emissão.",
                notaFiscalId = notaId
            });
        }
    }
}

public class CriarVendaRequest
{
    public string FormaPagamento { get; set; } = "01";
    public string? CpfConsumidor { get; set; }

    public List<CriarVendaItemRequest> Produtos { get; set; } = new();
}

public class CriarVendaItemRequest
{
    public Guid ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
}
