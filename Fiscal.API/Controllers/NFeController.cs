using Fiscal.API.Models.NFe;
using Fiscal.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fiscal.API.Controllers
{
    [ApiController]
    [Route("api/nfe")]
    public class NFeController : ControllerBase
    {
        private readonly FiscalService _fiscalService;

        public NFeController(FiscalService fiscalService)
        {
            _fiscalService = fiscalService;
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            try
            {
                var retorno = _fiscalService.ConsultarStatusNFe();

                return Ok(retorno);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    erro = ex.Message,
                    detalhe = ex.InnerException?.Message
                });
            }
        }


        [HttpPost("emitir")]
        public IActionResult Emitir([FromBody] EmitirNFeRequest request)
        {
            if (request.Produtos == null || request.Produtos.Count == 0)
            {
                return BadRequest(new
                {
                    erro = "Informe pelo menos um produto."
                });
            }

            var valorTotal = request.Produtos.Sum(p =>
                p.Quantidade * p.ValorUnitario);

            return Ok(new
            {
                mensagem = "Dados recebidos com sucesso.",
                destinatario = request.Destinatario.Nome,
                quantidadeItens = request.Produtos.Count,
                valorTotal
            });
        }

        [HttpPost("gerar-xml")]
        public IActionResult GerarXml([FromBody] EmitirNFeRequest request)
        {
            try
            {
                var retorno = _fiscalService.GerarNFe(request);
                return Ok(retorno);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    erro = ex.Message,
                    detalhe = ex.InnerException?.Message
                });
            }
        }


        [HttpPost("autorizar")]
        public IActionResult Autorizar([FromBody] EmitirNFeRequest request)
        {
            try
            {
                var resultado = _fiscalService.AutorizarNFe(request);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    erro = ex.Message,
                    detalhe = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("consultar/{chave}")]
        public IActionResult Consultar(string chave)
        {
            try
            {
                var resultado = _fiscalService.ConsultarNFe(chave);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    erro = ex.Message,
                    detalhe = ex.InnerException?.Message
                });
            }
        }
    }
}
