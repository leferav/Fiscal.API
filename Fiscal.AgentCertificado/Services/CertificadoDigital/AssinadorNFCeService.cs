using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Fiscal.Agent.Services.CertificadoDigital;

/// <summary>Assina o elemento infNFe de uma NFC-e (XMLDSig enveloped, RSA-SHA256).</summary>
public sealed class AssinadorNFCeService
{
    private const string NsNfe = "http://www.portalfiscal.inf.br/nfe";

    public string Assinar(string xmlSemAssinatura, string caminhoPfx, string senha)
    {
        if (string.IsNullOrWhiteSpace(xmlSemAssinatura))
            throw new ArgumentException("XML da NFC-e não informado.", nameof(xmlSemAssinatura));
        if (string.IsNullOrWhiteSpace(caminhoPfx) || !File.Exists(caminhoPfx))
            throw new FileNotFoundException("Certificado A1 não encontrado.", caminhoPfx);

        using var certificado = new X509Certificate2(
            caminhoPfx, senha, X509KeyStorageFlags.EphemeralKeySet);
        if (!certificado.HasPrivateKey)
            throw new InvalidOperationException("Certificado A1 sem chave privada.");
        if (DateTime.UtcNow < certificado.NotBefore.ToUniversalTime() ||
            DateTime.UtcNow > certificado.NotAfter.ToUniversalTime())
            throw new InvalidOperationException("Certificado A1 fora do prazo de validade.");

        using RSA? chave = certificado.GetRSAPrivateKey();
        if (chave is null)
            throw new InvalidOperationException("A assinatura NFC-e requer chave RSA.");

        var doc = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        doc.LoadXml(xmlSemAssinatura);
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("n", NsNfe);

        if (doc.DocumentElement?.LocalName != "NFe" ||
            doc.DocumentElement.NamespaceURI != NsNfe)
            throw new InvalidOperationException("A raiz do XML deve ser NFe no namespace fiscal.");

        var infNfe = doc.SelectSingleNode("/n:NFe/n:infNFe", ns) as XmlElement
            ?? throw new InvalidOperationException("Elemento infNFe não encontrado.");
        var id = infNfe.GetAttribute("Id");
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^NFe\d{44}$"))
            throw new InvalidOperationException("Id de infNFe inválido (esperado NFe + 44 dígitos).");
        if (doc.SelectSingleNode("/n:NFe/*[local-name()='Signature']", ns) != null)
            throw new InvalidOperationException("O XML já possui assinatura.");

        var signedXml = new SignedXml(doc) { SigningKey = chave };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        var reference = new Reference { Uri = "#" + id, DigestMethod = SignedXml.XmlDsigSHA256Url };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificado));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();
        doc.DocumentElement.AppendChild(doc.ImportNode(signedXml.GetXml(), true));

        // Verifica a assinatura local antes de devolver o XML.
        var verificador = new SignedXml(doc);
        var assinatura = doc.SelectSingleNode("/n:NFe/*[local-name()='Signature']", ns) as XmlElement
            ?? throw new InvalidOperationException("Assinatura XML não encontrada após geração.");
        verificador.LoadXml(assinatura);
        if (!verificador.CheckSignature(certificado, true))
            throw new CryptographicException("Falha na verificação da assinatura da NFC-e.");

        return doc.OuterXml;
    }
}
