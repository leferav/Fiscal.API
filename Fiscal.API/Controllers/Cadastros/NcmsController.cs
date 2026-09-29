using Fiscal.API.Data;
using Fiscal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers.Cadastros
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/cadastros/ncms")]
    public class NcmsController : ControllerBase
    {
        private readonly NcmService _ncmService;
        private readonly FiscalDbContext _context;

        public NcmsController(NcmService ncmService, FiscalDbContext context)
        {
            _ncmService = ncmService;
            _context = context;
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

        [HttpGet]
        public async Task<IActionResult> Pesquisar( [FromQuery] string? busca)
        {
            if (string.IsNullOrWhiteSpace(busca))
            {
                return Ok(Array.Empty<object>());
            }

            var texto = busca.Trim();

            var codigo =
                texto.Replace(".", "")
                     .Replace("-", "");

            var ncms = await _context.Ncms
                .AsNoTracking()
                .Where(x =>
                    x.Ativo &&
                    (
                        x.Codigo.StartsWith(codigo) ||
                        EF.Functions.ILike(
                            x.Descricao,
                            $"%{texto}%")
                    ))
                .OrderByDescending(x =>
                    EF.Functions.ILike(
                        x.Descricao,
                        $"{texto}%"))
                .ThenBy(x => x.Codigo)
                .Take(20)
                .Select(x => new
                {
                    x.Codigo,
                    x.Descricao
                })
                .ToListAsync();

            return Ok(ncms);
        }
    }
}