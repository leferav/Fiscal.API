using System.Net.Http.Json;

namespace Fiscal.API.Services.Email;

public class EmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public EmailService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task EnviarEmailAsync(
        string destinatarioEmail,
        string destinatarioNome,
        string assunto,
        string conteudoHtml)
    {
        var apiKey = _configuration["Brevo:ApiKey"];
        var remetenteEmail =
            _configuration["Brevo:RemetenteEmail"];
        var remetenteNome =
            _configuration["Brevo:RemetenteNome"];

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(remetenteEmail))
        {
            throw new InvalidOperationException(
                "Configurações do Brevo não encontradas.");
        }

        var payload = new
        {
            sender = new
            {
                name = remetenteNome ?? "Fiscal.API",
                email = remetenteEmail
            },
            to = new[]
            {
                new
                {
                    email = destinatarioEmail,
                    name = destinatarioNome
                }
            },
            subject = assunto,
            htmlContent = conteudoHtml
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.brevo.com/v3/smtp/email");

        request.Headers.Add("api-key", apiKey);

        request.Content = JsonContent.Create(payload);

        using var response =
            await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Falha ao enviar e-mail pelo Brevo. " +
                $"Status HTTP: {(int)response.StatusCode}");
        }
    }
}
