using Fiscal.API.Models.NFe;
using Fiscal.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fiscal.API.Controllers
{
    [ApiController]
    [Route("api/nfce")]
    public class NFCeController : ControllerBase
    {
        private readonly FiscalService _fiscalService;

        public NFCeController(FiscalService fiscalService)
        {
            _fiscalService = fiscalService;
        }

        [HttpPost("autorizar")]
        public IActionResult Autorizar([FromBody] EmitirNFeRequest request)
        {
            try
            {
                var resultado =
                    _fiscalService.AutorizarNFCe(request);

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
                var resultado =
                    _fiscalService.ConsultarNFCe(chave);

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