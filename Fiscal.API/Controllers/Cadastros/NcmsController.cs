using Fiscal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fiscal.API.Controllers.Cadastros
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/cadastros/ncms")]
    public class NcmsController : ControllerBase
    {
        private readonly NcmService _ncmService;

        public NcmsController(NcmService ncmService)
        {
            _ncmService = ncmService;
        }

        [HttpGet("teste-siscomex")]
        public async Task<IActionResult> TestarSiscomex()
        {
            var ncms = await _ncmService.ObterNcmsAsync();

            return Ok(new
            {
                quantidade = ncms.Count,
                primeiros = ncms.Take(10)
            });
        }

        [HttpPost("sincronizar")]
        public async Task<IActionResult> Sincronizar()
        {
            var resultado =
                await _ncmService.SincronizarNcmsAsync();

            return Ok(resultado);
        }
    }
}