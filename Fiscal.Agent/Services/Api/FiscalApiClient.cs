using System.Net.Http.Json;
using System.Net;

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
                $"NÃ£o foi possÃ­vel vincular o Fiscal.Agent. " +
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
                "A Fiscal.API retornou uma resposta de vinculaÃ§Ã£o invÃ¡lida.");
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
    public async Task<SolicitacaoEmissaoAgent?> BuscarEmissaoPendenteAsync(
        Guid agenteId, string credencial, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"api/agentes/{agenteId}/emissoes/pendente");
        request.Headers.Add("X-Agent-Credential", credencial);
        using var response = await _httpClient.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        if (!response.IsSuccessStatusCode)
        {
            var detalhe = await response.Content.ReadAsStringAsync(ct);

            throw new Exception(
                $"Erro ao buscar NFC-e pendente. " +
                $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        return await response.Content.ReadFromJsonAsync<SolicitacaoEmissaoAgent>(
            cancellationToken: ct);
    }

    public async Task EnviarResultadoEmissaoAsync(
        Guid agenteId, string credencial, Guid solicitacaoId,
        ResultadoEmissaoAgent resultado, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"api/agentes/{agenteId}/emissoes/{solicitacaoId}/resultado");
        request.Headers.Add("X-Agent-Credential", credencial);
        request.Content = JsonContent.Create(resultado);
        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detalhe = await response.Content.ReadAsStringAsync(ct);

            throw new Exception(
                $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
    }

}

public class VincularAgenteResponse
{
    public Guid AgenteId { get; set; }

    public string Credencial { get; set; } = string.Empty;

    public string? Mensagem { get; set; }
}

public class SolicitacaoEmissaoAgent
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public int Modelo { get; set; }
    public int Serie { get; set; }
    public long Numero { get; set; }
    public string Xml { get; set; } = string.Empty;
    public string CscId { get; set; } = string.Empty;
    public string Csc { get; set; } = string.Empty;
}

public class ResultadoEmissaoAgent
{
    public bool Sucesso { get; set; }
    public int? CStat { get; set; }
    public string? Motivo { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? Protocolo { get; set; }
    public string? XmlEnvio { get; set; }
    public string? XmlRetorno { get; set; }
    public string? XmlAutorizado { get; set; }
}
