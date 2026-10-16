using Fiscal.API.Data;
using Microsoft.EntityFrameworkCore;

public sealed class MonitoramentoEmissoesService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitoramentoEmissoesService> _logger;

    public MonitoramentoEmissoesService(
        IServiceScopeFactory scopeFactory,
        ILogger<MonitoramentoEmissoesService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(" ");
        _logger.LogInformation("         ================         Monitoramento automático de NFC-e iniciado.          ================         ");
        _logger.LogInformation(" ");

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                // Substitua FiscalDbContext pelo nome real
                // do DbContext utilizado na sua Fiscal.API.
                var context = scope.ServiceProvider
                    .GetRequiredService<FiscalDbContext>();

                var limite = DateTime.UtcNow.AddSeconds(-60);

                var travadas = await context.NotasFiscais
                    .AsNoTracking()
                    .Where(x =>
                        x.Modelo == 65 &&
                        x.Status == "EM_PROCESSAMENTO" &&
                        x.ReservadaEm != null &&
                        x.ReservadaEm < limite)
                    .Select(x => new
                    {
                        x.Id,
                        x.EmpresaId,
                        x.Numero,
                        x.RecebidaPeloAgenteEm
                    })
                    .ToListAsync(stoppingToken);

                foreach (var nota in travadas)
                {
                    _logger.LogWarning(
                        "NFC-e {Numero} da empresa {EmpresaId} " +
                        "em processamento há mais de 60 segundos. " +
                        "Confirmação do Agent: {Confirmacao}. " +
                        "Requer conciliação.",
                        nota.Numero,
                        nota.EmpresaId,
                        nota.RecebidaPeloAgenteEm);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro no monitoramento de emissões NFC-e.");
            }
        }
    }
}