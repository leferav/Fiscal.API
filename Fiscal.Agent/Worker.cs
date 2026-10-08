using Fiscal.Agent.Services.Api;
using Fiscal.Agent.Services.CertificadoDigital;
using Fiscal.Agent.Services.Configuracao;

namespace Fiscal.Agent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CertificadoService _certificadoService;
    private readonly ConfiguracaoLocalService _configuracaoLocalService;
    private readonly FiscalApiClient _fiscalApiClient;

    public Worker(
        ILogger<Worker> logger,
        CertificadoService certificadoService,
        ConfiguracaoLocalService configuracaoLocalService,
        FiscalApiClient fiscalApiClient)
    {
        _logger = logger;
        _certificadoService = certificadoService;
        _configuracaoLocalService = configuracaoLocalService;
        _fiscalApiClient = fiscalApiClient;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("=== Fiscal.Agent ===");
            Console.WriteLine();

            var configuracao =
                _configuracaoLocalService.Carregar();

            if (configuracao == null)
            {
                Console.WriteLine(
                    "Primeira configuração do Fiscal.Agent.");
                Console.WriteLine();

                Console.Write(
                    "Caminho do certificado PFX: ");

                var caminhoPfx =
                    Console.ReadLine() ?? string.Empty;

                Console.Write(
                    "Senha do certificado: ");

                var senha = LerSenha();

                Console.WriteLine();
                Console.WriteLine(
                    "Validando certificado...");

                // Valida antes de salvar
                _certificadoService.LerCertificado(
                    caminhoPfx,
                    senha);

                _configuracaoLocalService.SalvarCertificado(
                    caminhoPfx,
                    senha);

                Console.WriteLine();
                Console.WriteLine(
                    "Configuração salva com sucesso.");

                configuracao =
                    _configuracaoLocalService.Carregar();

                if (configuracao == null)
                {
                    throw new Exception(
                        "Não foi possível carregar a configuração salva.");
                }
            }

            var senhaCertificado =
                _configuracaoLocalService.DesprotegerSenha(
                    configuracao.SenhaProtegida);

            var certificado =
                _certificadoService.LerCertificado(
                    configuracao.CaminhoCertificado,
                    senhaCertificado);

            Console.WriteLine();
            Console.WriteLine(
                "Certificado local carregado com sucesso.");

            Console.WriteLine(
                $"Titular: {certificado.Titular}");

            Console.WriteLine(
                $"CNPJ: {certificado.Cnpj}");

            Console.WriteLine(
                $"Válido até: {certificado.ValidoAte:dd/MM/yyyy}");

            Console.WriteLine(
                $"Possui chave privada: {certificado.PossuiChavePrivada}");



            configuracao =
                _configuracaoLocalService.Carregar();

            if (configuracao == null)
            {
                throw new Exception(
                    "Configuração local não encontrada.");
            }

            var agenteVinculado =
                configuracao.AgenteId.HasValue &&
                configuracao.AgenteId.Value != Guid.Empty &&
                !string.IsNullOrWhiteSpace(
                    configuracao.CredencialAgenteProtegida);

            if (!agenteVinculado)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "Fiscal.Agent ainda não está vinculado.");

                Console.Write(
                    "Informe o código de vinculação: ");

                var codigo =
                    Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    throw new Exception(
                        "Código de vinculação não informado.");
                }

                var nomeMaquina =
                    Environment.MachineName;

                Console.WriteLine();
                Console.WriteLine(
                    "Vinculando Fiscal.Agent...");

                var resultado =
                    await _fiscalApiClient.VincularAsync(
                        codigo,
                        $"Fiscal.Agent - {nomeMaquina}",
                        nomeMaquina,
                        stoppingToken);

                _configuracaoLocalService
                    .SalvarCredencialAgente(
                        resultado.AgenteId,
                        resultado.Credencial);

                Console.WriteLine();
                Console.WriteLine(
                    "Fiscal.Agent vinculado com sucesso.");

                Console.WriteLine(
                    $"AgenteId: {resultado.AgenteId}");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine(
                    "Fiscal.Agent já está vinculado.");

                Console.WriteLine(
                    $"AgenteId: {configuracao.AgenteId}");
            }




            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Fiscal.Agent aguardando solicitações...");
            Console.WriteLine("Heartbeat automático iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var configuracaoAtual =
                        _configuracaoLocalService.Carregar();

                    if (configuracaoAtual?.AgenteId is not Guid agenteId ||
                        string.IsNullOrWhiteSpace(
                            configuracaoAtual.CredencialAgenteProtegida))
                    {
                        throw new InvalidOperationException(
                            "Fiscal.Agent não está vinculado.");
                    }

                    var credencial =
                        _configuracaoLocalService.DesprotegerCredencialAgente(
                            configuracaoAtual.CredencialAgenteProtegida);

                    await _fiscalApiClient.EnviarHeartbeatAsync(
                        agenteId,
                        credencial,
                        stoppingToken);

                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss}] Heartbeat enviado com sucesso.");
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Falha ao enviar heartbeat: {Mensagem}",
                        ex.Message);
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(30),
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento normal.
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro na execução do Fiscal.Agent.");
        }
    }

    private static string LerSenha()
    {
        var senha = string.Empty;

        while (true)
        {
            var tecla =
                Console.ReadKey(intercept: true);

            if (tecla.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (senha.Length > 0)
                    senha = senha[..^1];

                continue;
            }

            senha += tecla.KeyChar;
        }

        return senha;
    }
}