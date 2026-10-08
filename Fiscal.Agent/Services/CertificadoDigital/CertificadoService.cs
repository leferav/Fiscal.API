using Fiscal.Agent.Models;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Fiscal.Agent.Services.CertificadoDigital;

public class CertificadoService
{
    public CertificadoInfo LerCertificado(
        string caminhoPfx,
        string senha)
    {
        if (string.IsNullOrWhiteSpace(caminhoPfx))
        {
            throw new Exception(
                "Caminho do certificado não informado.");
        }

        if (!File.Exists(caminhoPfx))
        {
            throw new FileNotFoundException(
                "Arquivo do certificado não encontrado.",
                caminhoPfx);
        }

        var certificado = new X509Certificate2(
            caminhoPfx,
            senha,
            X509KeyStorageFlags.EphemeralKeySet);

        if (!certificado.HasPrivateKey)
        {
            throw new Exception(
                "O certificado não possui chave privada.");
        }

        return new CertificadoInfo
        {
            Titular = certificado.GetNameInfo(
                X509NameType.SimpleName,
                false),

            Cnpj = ExtrairCnpj(
                certificado.Subject),

            Emissor = certificado.GetNameInfo(
                X509NameType.SimpleName,
                true),

            Thumbprint = certificado.Thumbprint,

            ValidoDe =
                certificado.NotBefore.ToUniversalTime(),

            ValidoAte =
                certificado.NotAfter.ToUniversalTime(),

            PossuiChavePrivada =
                certificado.HasPrivateKey
        };
    }

    private static string? ExtrairCnpj(
        string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return null;

        var match = Regex.Match(
            subject,
            @"(?<!\d)\d{14}(?!\d)");

        return match.Success
            ? match.Value
            : null;
    }
}