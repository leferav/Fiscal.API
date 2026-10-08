
using Fiscal.API.Models.Agente;
using NFe.Utils.NFe;

namespace Fiscal.API.Services.NFCe;

public class NFCeXmlPreparacaoService
{
    public PayloadEmissaoNFCe Preparar(
        global::NFe.Classes.NFe nfce,
        Guid empresaId)
    {
        ArgumentNullException.ThrowIfNull(nfce);

        if (nfce.infNFe?.ide == null)
        {
            throw new InvalidOperationException(
                "Identificação da NFC-e não encontrada.");
        }

        var ide = nfce.infNFe.ide;

        if ((int)ide.mod != 65)
        {
            throw new InvalidOperationException(
                "O documento informado não é uma NFC-e.");
        }

        if (ide.serie <= 0 || ide.nNF <= 0)
        {
            throw new InvalidOperationException(
                "Série ou número da NFC-e inválido.");
        }

        // Serializa a NFC-e antes da assinatura digital.
        var xmlSemAssinatura = nfce.ObterXmlString();

        if (string.IsNullOrWhiteSpace(xmlSemAssinatura))
        {
            throw new InvalidOperationException(
                "Não foi possível gerar o XML da NFC-e.");
        }

        return new PayloadEmissaoNFCe
        {
            VersaoContrato = 1,
            EmpresaId = empresaId,
            Modelo = 65,
            Ambiente = (short)(int)ide.tpAmb,
            Serie = ide.serie,
            Numero = checked((int)ide.nNF),
            XmlSemAssinatura = xmlSemAssinatura
        };
    }
}
