
using Fiscal.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fiscal.API.Services;

public class NumeracaoFiscalService
{
    private readonly FiscalDbContext _context;

    public NumeracaoFiscalService(FiscalDbContext context)
    {
        _context = context;
    }



    public async Task<long> ReservarProximoNumeroNFCeAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        var transacao = _context.Database.CurrentTransaction;

        if (transacao == null)
            throw new InvalidOperationException(
                "A reserva da numeração exige uma transação ativa.");

        var conexao = _context.Database.GetDbConnection();

        await using var comando = conexao.CreateCommand();

        comando.Transaction = transacao.GetDbTransaction();

        comando.CommandText = """
        UPDATE configuracoes_fiscais
        SET "ProximoNumeroNFCe" = "ProximoNumeroNFCe" + 1
        WHERE "EmpresaId" = @empresaId
        RETURNING "ProximoNumeroNFCe" - 1
        """;

        var parametro = comando.CreateParameter();
        parametro.ParameterName = "empresaId";
        parametro.Value = empresaId;

        comando.Parameters.Add(parametro);

        var resultado = await comando.ExecuteScalarAsync(cancellationToken);

        if (resultado == null || resultado == DBNull.Value)
            throw new InvalidOperationException(
                "Configuração fiscal não encontrada.");

        var numeroReservado = Convert.ToInt64(resultado);

        if (numeroReservado <= 0)
            throw new InvalidOperationException(
                "Numeração fiscal inválida.");

        return numeroReservado;
    }


}
