
using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Services.NFCe;

public class NumeracaoNFCeService
{
    private readonly FiscalDbContext _context;

    public NumeracaoNFCeService(FiscalDbContext context)
    {
        _context = context;
    }

    public async Task<ConfiguracaoFiscal> ObterConfiguracaoComBloqueioAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction == null)
        {
            throw new InvalidOperationException(
                "A reserva exige uma transação ativa.");
        }

        var configuracao = await _context.ConfiguracoesFiscais
            .FromSqlInterpolated($"""
                SELECT *
                FROM configuracoes_fiscais
                WHERE "EmpresaId" = {empresaId}
                FOR UPDATE
                """)
            .AsTracking()
            .ToListAsync(cancellationToken);

        var resultado = configuracao.SingleOrDefault();

        if (resultado == null)
        {
            throw new InvalidOperationException(
                "Configuração fiscal não encontrada.");
        }

        if (resultado.ProximoNumeroNFCe <= 0 ||
            resultado.ProximoNumeroNFCe > 999999999)
        {
            throw new InvalidOperationException(
                "Número da NFC-e fora do intervalo permitido.");
        }

        return resultado;
    }




    public async Task<object> TestarReservaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        await using var transacao =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        var configuracao =
            await ObterConfiguracaoComBloqueioAsync(
                empresaId, cancellationToken);

        var numeroAntes = configuracao.ProximoNumeroNFCe;

        configuracao.ProximoNumeroNFCe++;

        await _context.SaveChangesAsync(cancellationToken);

        var numeroDurante = configuracao.ProximoNumeroNFCe;

        // Teste: nenhuma alteração será confirmada.
        await transacao.RollbackAsync(cancellationToken);

        // Evita manter no DbContext a entidade com valor
        // alterado após o rollback.
        _context.Entry(configuracao).State =
            EntityState.Detached;

        return new
        {
            empresaId,
            numeroAntes,
            numeroDurante,
            rollbackExecutado = true
        };
    }

}
