
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


}
