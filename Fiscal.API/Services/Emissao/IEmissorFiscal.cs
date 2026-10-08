using Fiscal.API.Services.Emissao.Models;

namespace Fiscal.API.Services.Emissao
{
    public interface IEmissorFiscal
    {
        Task<ResultadoEmissao> EmitirNFCeAsync(
            SolicitacaoEmissao solicitacao,
            CancellationToken cancellationToken = default);
    }
}