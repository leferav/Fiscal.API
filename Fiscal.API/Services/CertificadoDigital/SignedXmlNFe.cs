using System.Security.Cryptography.Xml;
using System.Xml;

namespace Fiscal.API.Services.CertificadoDigital;

public sealed class SignedXmlNFe : SignedXml
{
    public SignedXmlNFe(XmlDocument documento)
        : base(documento)
    {
    }

    public override XmlElement? GetIdElement(
        XmlDocument? document,
        string idValue)
    {
        var elemento = base.GetIdElement(document, idValue);

        if (elemento != null)
            return elemento;

        if (document == null)
            return null;

        var nodes = document.GetElementsByTagName(
            "infNFe",
            "http://www.portalfiscal.inf.br/nfe");

        foreach (XmlNode node in nodes)
        {
            if (node is XmlElement infNFe &&
                infNFe.GetAttribute("Id") == idValue)
            {
                return infNFe;
            }
        }

        return null;
    }
}