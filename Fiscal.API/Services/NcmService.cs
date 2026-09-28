using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Integracoes.Ncm;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace Fiscal.API.Services
{
    public class NcmService
    {
        private readonly HttpClient _httpClient;
        private readonly FiscalDbContext _context;

        private const string UrlNcm =
            "https://portalunico.siscomex.gov.br/classif/api/publico/nomenclatura/download/json";

        public NcmService(HttpClient httpClient, FiscalDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        public async Task<List<NcmSiscomexDto>> ObterNcmsAsync()
        {
            var response = await _httpClient.GetAsync(UrlNcm);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var resultado =
                JsonSerializer.Deserialize<NcmSiscomexResponseDto>(
                    json,
                    options);

            if (resultado?.Nomenclaturas == null)
            {
                return new List<NcmSiscomexDto>();
            }

            var ncmsValidos = resultado.Nomenclaturas
                .Where(x => !string.IsNullOrWhiteSpace(x.Codigo))
                .Select(x => new
                {
                    Ncm = x,
                    CodigoLimpo = x.Codigo.Replace(".", "").Trim()
                })
                .Where(x =>
                    x.CodigoLimpo.Length == 8 &&
                    x.CodigoLimpo.All(char.IsDigit))
                .Select(x =>
                {
                    x.Ncm.Codigo = x.CodigoLimpo;
                    return x.Ncm;
                })
                .ToList();

            return ncmsValidos;
        }

        public async Task<object> SincronizarNcmsAsync()
        {
            var ncmsSiscomex = await ObterNcmsAsync();

            var ncmsBanco = await _context.Ncms
                .ToDictionaryAsync(x => x.Codigo);

            var inseridos = 0;
            var atualizados = 0;

            foreach (var item in ncmsSiscomex)
            {
                DateTime? dataInicio = ConverterData(item.DataInicio);
                DateTime? dataFim = ConverterData(item.DataFim);

                if (ncmsBanco.TryGetValue(
                    item.Codigo,
                    out var ncmExistente))
                {
                    ncmExistente.Descricao =
                        item.Descricao.Trim();

                    ncmExistente.DataInicio = dataInicio;
                    ncmExistente.DataFim = dataFim;
                    ncmExistente.Ativo = true;

                    atualizados++;

                    continue;
                }

                var ncm = new Ncm
                {
                    Id = Guid.NewGuid(),
                    Codigo = item.Codigo,
                    Descricao = item.Descricao.Trim(),
                    DataInicio = dataInicio,
                    DataFim = dataFim,
                    Ativo = true
                };

                _context.Ncms.Add(ncm);

                ncmsBanco.Add(ncm.Codigo, ncm);

                inseridos++;
            }

            await _context.SaveChangesAsync();

            return new
            {
                recebidos = ncmsSiscomex.Count,
                inseridos,
                atualizados
            };
        }

        private static DateTime? ConverterData(string? data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            if (DateTime.TryParseExact(
                data,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dataConvertida))
            {
                return DateTime.SpecifyKind(
                    dataConvertida,
                    DateTimeKind.Utc);
            }

            return null;
        }
    }
}