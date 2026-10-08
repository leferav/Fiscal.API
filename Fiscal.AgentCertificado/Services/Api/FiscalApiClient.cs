
using System.Net.Http.Json;
using Fiscal.Agent.Models;

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

        using var response = await _httpClient.PostAsJsonAsync(
            "api/agentes/vincular",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var conteudo = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new Exception(
                $"Não foi possível vincular o Fiscal.Agent. " +
                $"HTTP {(int)response.StatusCode}: {conteudo}");
        }

        var resultado = await response.Content
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

    public async Task SincronizarCertificadoAsync(
        Guid agenteId,
        string credencial,
        CertificadoInfo certificado,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(certificado);

        var request = new
        {
            credencial,
            titular = certificado.Titular,
            cnpj = certificado.Cnpj,
            emissor = certificado.Emissor,
            thumbprint = certificado.Thumbprint,
            validoDe = certificado.ValidoDe,
            validoAte = certificado.ValidoAte
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"api/agentes/{agenteId}/certificado",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "Erro ao sincronizar os metadados do certificado. " +
                $"HTTP {(int)response.StatusCode}");
        }
    }


    public async Task<SolicitacaoEmissaoResponse?> ObterProximaSolicitacaoAsync(
        Guid agenteId,
        string credencial,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"api/agentes/{agenteId}/solicitacoes/proxima",
            new { credencial },
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var conteudo = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Erro ao consultar fila. HTTP {(int)response.StatusCode}: {conteudo}");
        }

        var solicitacao = await response.Content
            .ReadFromJsonAsync<SolicitacaoEmissaoResponse>(
                cancellationToken: cancellationToken);

        if (solicitacao == null ||
            solicitacao.Id == Guid.Empty ||
            solicitacao.TentativaId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A API retornou uma solicitação inválida.");
        }

        return solicitacao;
    }

    public async Task EnviarResultadoSolicitacaoAsync(
        Guid agenteId,
        Guid solicitacaoId,
        ResultadoEmissaoRequest resultado,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        using var response = await _httpClient.PostAsJsonAsync(
            $"api/agentes/{agenteId}/solicitacoes/{solicitacaoId}/resultado",
            resultado,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var conteudo = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Erro ao registrar resultado. HTTP {(int)response.StatusCode}: {conteudo}");
        }
    }

}

public class VincularAgenteResponse
{
    public Guid AgenteId { get; set; }

    public string Credencial { get; set; } = string.Empty;

    public string? Mensagem { get; set; }
}

public class SolicitacaoEmissaoResponse
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public Guid TentativaId { get; set; }

    public short Modelo { get; set; }

    public short Ambiente { get; set; }

    public int Serie { get; set; }

    public int Numero { get; set; }

    public string PayloadJson { get; set; } = string.Empty;
}

public class ResultadoEmissaoRequest
{
    public string Credencial { get; set; } = string.Empty;

    public Guid TentativaId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ChaveAcesso { get; set; }

    public string? Protocolo { get; set; }

    public int? CStat { get; set; }

    public string? XMotivo { get; set; }

    public string? XmlAutorizado { get; set; }

    public string? Erro { get; set; }
}

