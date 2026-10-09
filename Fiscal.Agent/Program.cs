using Fiscal.Agent;
using Fiscal.Agent.Services.Api;
using Fiscal.Agent.Services.CertificadoDigital;
using Fiscal.Agent.Services.Configuracao;
using Fiscal.Agent.Services.NFe;
using Fiscal.Agent.Services.NFCe;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<CertificadoService>();
builder.Services.AddSingleton<ConfiguracaoLocalService>();
builder.Services.AddSingleton<ZeusConfigurationFactory>();
builder.Services.AddSingleton<NFCeEmissaoService>();

builder.Services.AddHttpClient<FiscalApiClient>((serviceProvider, client) =>
{
    var configuration =
        serviceProvider.GetRequiredService<IConfiguration>();

    var baseUrl =
        configuration["FiscalApi:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new Exception(
            "FiscalApi:BaseUrl não configurada.");
    }

    client.BaseAddress =
        new Uri(baseUrl);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();