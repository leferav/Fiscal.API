using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace Fiscal.API.Controllers.Consultas;

[ApiController]
[Authorize]
[Route("api/consultas/cep")]
public class CepController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public CepController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("{cep}")]
    public async Task<IActionResult> Consultar(string cep)
    {
        var cepNumerico = new string(
            (cep ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray()
        );

        if (cepNumerico.Length != 8)
        {
            return BadRequest(new
            {
                mensagem = "Informe um CEP válido com 8 dígitos."
            });
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"https://viacep.com.br/ws/{cepNumerico}/json/"
            );

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return BadRequest(new
                {
                    mensagem = "CEP inválido."
                });
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        mensagem =
                            "Não foi possível consultar o serviço de CEP."
                    }
                );
            }

            var resultado =
                await response.Content
                    .ReadFromJsonAsync<ViaCepResponse>();

            if (resultado == null ||
                resultado.Erro == "true")
            {
                return NotFound(new
                {
                    mensagem = "CEP não encontrado."
                });
            }

            int.TryParse(
                resultado.Ibge,
                out var codigoMunicipio
            );

            return Ok(new
            {
                cep = cepNumerico,
                logradouro = resultado.Logradouro ?? "",
                bairro = resultado.Bairro ?? "",
                municipio = resultado.Localidade ?? "",
                uf = resultado.Uf ?? "",
                codigoMunicipio
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    mensagem =
                        "O serviço de consulta de CEP está indisponível."
                }
            );
        }
    }

    private sealed class ViaCepResponse
    {
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Bairro { get; set; }
        public string? Localidade { get; set; }
        public string? Uf { get; set; }
        public string? Ibge { get; set; }
        public string? Erro { get; set; }
    }
}