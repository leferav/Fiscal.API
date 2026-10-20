
using System.Security.Cryptography;

namespace Fiscal.API.Services.CertificadoDigital;

public class CertificadoCriptografiaService
{
    private readonly byte[] _chave;

    public CertificadoCriptografiaService(IConfiguration configuration)
    {
        var chaveBase64 = configuration["Certificados:ChaveCriptografia"];

        if (string.IsNullOrWhiteSpace(chaveBase64))
            throw new InvalidOperationException(
                "Chave de criptografia dos certificados não configurada.");

        try
        {
            _chave = Convert.FromBase64String(chaveBase64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "A chave de criptografia deve estar em Base64.");
        }

        if (_chave.Length != 32)
            throw new InvalidOperationException(
                "A chave de criptografia deve possuir 32 bytes (256 bits).");
    }

    public (byte[] Conteudo, byte[] Nonce, byte[] Tag) Criptografar(
        byte[] dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var nonce = RandomNumberGenerator.GetBytes(12);
        var conteudo = new byte[dados.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_chave, 16);

        aes.Encrypt(nonce, dados, conteudo, tag);

        return (conteudo, nonce, tag);
    }

    public byte[] Descriptografar(
        byte[] conteudo,
        byte[] nonce,
        byte[] tag)
    {
        var dados = new byte[conteudo.Length];

        using var aes = new AesGcm(_chave, 16);

        aes.Decrypt(nonce, conteudo, tag, dados);

        return dados;
    }
}
