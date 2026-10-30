using Fiscal.API.Data;
using Fiscal.API.Models.Database;

namespace Fiscal.API.Services;

// Usa um DbContext independente: um erro no fluxo de emissão não impede a tentativa de log.
public sealed class HistoricoFiscalService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HistoricoFiscalService> _logger;

    public HistoricoFiscalService(IServiceScopeFactory scopeFactory, ILogger<HistoricoFiscalService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RegistrarAsync(LogFiscal registro)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FiscalDbContext>();
            db.LogsFiscais.Add(registro);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // O histórico é diagnóstico: nunca deve mascarar o resultado fiscal.
            _logger.LogError(ex,
                "Falha ao persistir log fiscal. EmpresaId={EmpresaId} NotaFiscalId={NotaFiscalId} Evento={Evento}",
                registro.EmpresaId, registro.NotaFiscalId, registro.Evento);
        }
    }
}
