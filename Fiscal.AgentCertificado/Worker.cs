using Fiscal.Agent.Services.Api;
using Fiscal.Agent.Services.CertificadoDigital;
using Fiscal.Agent.Services.Configuracao;
using Fiscal.Agent.Services.Emissao;

namespace Fiscal.Agent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CertificadoService _certificadoService;
    private readonly ConfiguracaoLocalService _configuracaoLocalService;
    private readonly FiscalApiClient _fiscalApiClient;
    private readonly ProcessadorEmissaoService _processadorEmissaoService;

    public Worker(
        ILogger<Worker> logger,
        CertificadoService certificadoService,
        ConfiguracaoLocalService configuracaoLocalService,
        FiscalApiClient fiscalApiClient,
        ProcessadorEmissaoService processadorEmissaoService)
    {
        _logger = logger;
        _certificadoService = certificadoService;
        _configuracaoLocalService = configuracaoLocalService;
        _fiscalApiClient = fiscalApiClient;
        _processadorEmissaoService = processadorEmissaoService;
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
            // Sincroniza os metadados uma vez na inicialização.
            // Falhas não impedem o heartbeat.
            try
            {
                var configuracaoAtual = _configuracaoLocalService.Carregar();
                if (configuracaoAtual?.AgenteId is not Guid agenteId ||
                    agenteId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(configuracaoAtual.CredencialAgenteProtegida))
                {
                    throw new InvalidOperationException("Agent não vinculado.");
                }

                var credencial = _configuracaoLocalService.DesprotegerCredencialAgente(
                    configuracaoAtual.CredencialAgenteProtegida);

                await _fiscalApiClient.SincronizarCertificadoAsync(
                    agenteId, credencial, certificado, stoppingToken);

                Console.WriteLine("Metadados do certificado sincronizados com sucesso.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Falha ao sincronizar certificado: {Mensagem}", ex.Message);
            }

            Console.WriteLine("Fiscal.Agent aguardando solicitações...");
            Console.WriteLine("Heartbeat automático iniciado.");

            // Desativado por padrão. Ativar apenas para teste controlado
            // com solicitações de homologação criadas especificamente para isso.
            var consultarFilaTeste = string.Equals(
                Environment.GetEnvironmentVariable("FISCAL_AGENT_CONSULTAR_FILA_TESTE"),
                "true", StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation(
                "Consulta da fila em modo de teste: {Ativa}. " +
                "Assinatura e transmissão SEFAZ desabilitadas.",
                consultarFilaTeste);

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

                    if (consultarFilaTeste)
                    {
                        // A consulta RESERVA a solicitação na API.
                        // Nunca ativar contra uma fila real de emissão.
                        var solicitacao = await _fiscalApiClient
                            .ObterProximaSolicitacaoAsync(
                                agenteId, credencial, stoppingToken);

                        if (solicitacao != null)
                        {
                            string detalhe;
                            try
                            {
                                _processadorEmissaoService.ValidarSolicitacao(solicitacao);
                                detalhe = "Simulação concluída: payload validado; sem emissão SEFAZ.";
                                _logger.LogInformation(
                                    "Solicitação {Id}, número {Numero}: payload validado (simulação).",
                                    solicitacao.Id, solicitacao.Numero);
                            }
                            catch (Exception ex)
                            {
                                detalhe = "Falha na validação da solicitação: " + ex.Message;
                                _logger.LogWarning(
                                    "Solicitação {Id}: {Mensagem}",
                                    solicitacao.Id, ex.Message);
                            }

                            // O status ERRO é intencional no teste: não foi emitida NFC-e.
                            // A tentativa deve ser encerrada para não ficar PROCESSANDO.
                            await _fiscalApiClient.EnviarResultadoSolicitacaoAsync(
                                agenteId,
                                solicitacao.Id,
                                new ResultadoEmissaoRequest
                                {
                                    Credencial = credencial,
                                    TentativaId = solicitacao.TentativaId,
                                    Status = "ERRO",
                                    Erro = detalhe
                                },
                                stoppingToken);
                        }
                    }
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