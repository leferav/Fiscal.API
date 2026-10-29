using Fiscal.API.Data;
using Fiscal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Text;

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
            var resultado = await _ncmService.SincronizarNcmsAsync();
            return Ok(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> Pesquisar([FromQuery] string? busca)
        {
            if (string.IsNullOrWhiteSpace(busca))
                return Ok(Array.Empty<object>());

            var texto = busca.Trim();
            var textoSemAcentos = RemoverAcentos(texto);
            var codigo = texto.Replace(".", "").Replace("-", "");

            // Escapa curingas do LIKE para que %, _ e \\ sejam tratados como texto.
            var termo = textoSemAcentos.Replace("\\", "\\\\")
                                       .Replace("%", "\\%")
                                       .Replace("_", "\\_");
            var codigoEscapado = codigo.Replace("\\", "\\\\")
                                      .Replace("%", "\\%")
                                      .Replace("_", "\\_");
            var buscaDescricao = $"%{termo}%";
            var inicioDescricao = $"{termo}%";
            var inicioCodigo = $"{codigoEscapado}%";

            // FromSqlInterpolated transforma os valores interpolados em parâmetros SQL.
            // A função unaccent é executada pelo PostgreSQL, não pelo C#.
            var registros = await _context.Ncms
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM ncms
                    WHERE ""Ativo"" = TRUE
                      AND (
                          ""Codigo"" LIKE {inicioCodigo}
                          OR unaccent(""Descricao"") ILIKE {buscaDescricao}
                      )
                    ORDER BY
                      CASE WHEN unaccent(""Descricao"") ILIKE {inicioDescricao}
                           THEN 0 ELSE 1 END,
                      ""Codigo""
                    LIMIT 20")
                .AsNoTracking()
                .ToListAsync();

            return Ok(registros.Select(x => new
            {
                x.Codigo,
                x.Descricao
            }));
        }

        private static string RemoverAcentos(string texto)
        {
            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var resultado = new StringBuilder();

            foreach (var caractere in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caractere)
                    != UnicodeCategory.NonSpacingMark)
                {
                    resultado.Append(caractere);
                }
            }

            return resultado.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
