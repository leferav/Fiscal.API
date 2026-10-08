using System.Net.Http.Json;

namespace Fiscal.Agent.Services.Api;

public class FiscalApiClient
{
    private readonly HttpClient _httpClient;

    public FiscalApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VincularAgenteResponse> VincularAsync(
        string codigo,
        string nome,
        string identificadorMaquina,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            codigo,
            nome,
            identificadorMaquina
        };

        var response =
            await _httpClient.PostAsJsonAsync(
                "api/agentes/vincular",
                request,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var conteudo =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            throw new Exception(
                $"Não foi possível vincular o Fiscal.Agent. " +
                $"HTTP {(int)response.StatusCode}: {conteudo}");
        }

        var resultado =
            await response.Content
                .ReadFromJsonAsync<VincularAgenteResponse>(
                    cancellationToken: cancellationToken);

        if (resultado == null ||
            resultado.AgenteId == Guid.Empty ||
            string.IsNullOrWhiteSpace(resultado.Credencial))
        {
            throw new Exception(
                "A Fiscal.API retornou uma resposta de vinculação inválida.");
        }

        return resultado;
    }

    public async Task EnviarHeartbeatAsync(
    Guid agenteId,
    string credencial,
    CancellationToken cancellationToken = default)
    {
        var request = new
        {
            credencial
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"api/agentes/{agenteId}/heartbeat",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Erro ao enviar heartbeat. HTTP {(int)response.StatusCode}");
        }
    }
}

public class VincularAgenteResponse
{
    public Guid AgenteId { get; set; }

    public string Credencial { get; set; } = string.Empty;

    public string? Mensagem { get; set; }
}