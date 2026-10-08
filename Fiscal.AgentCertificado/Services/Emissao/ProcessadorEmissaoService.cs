
using System.Text.Json;
using Fiscal.Agent.Services.Api;

namespace Fiscal.Agent.Services.Emissao;

public class ProcessadorEmissaoService
{
    public void ValidarSolicitacao(
        SolicitacaoEmissaoResponse solicitacao)
    {
        ArgumentNullException.ThrowIfNull(solicitacao);

        if (solicitacao.Id == Guid.Empty)
            throw new InvalidOperationException(
                "Identificador da solicitação inválido.");

        if (solicitacao.EmpresaId == Guid.Empty)
            throw new InvalidOperationException(
                "Empresa não informada.");

        if (solicitacao.TentativaId == Guid.Empty)
            throw new InvalidOperationException(
                "Tentativa de processamento inválida.");

        if (solicitacao.Modelo != 65)
            throw new InvalidOperationException(
                "Somente NFC-e modelo 65 é suportada.");

        if (solicitacao.Serie <= 0 ||
            solicitacao.Numero <= 0)
        {
            throw new InvalidOperationException(
                "Série ou número fiscal inválido.");
        }

        if (string.IsNullOrWhiteSpace(
            solicitacao.PayloadJson))
        {
            throw new InvalidOperationException(
                "Payload da emissão não informado.");
        }

        using var documento = JsonDocument.Parse(
            solicitacao.PayloadJson);

        if (documento.RootElement.ValueKind !=
            JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "O payload deve ser um objeto JSON.");
        }
    }
}
