
namespace Fiscal.API.Models.Database;

public class CertificadoDigitalArquivo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CertificadoDigitalId { get; set; }

    public CertificadoDigital CertificadoDigital { get; set; } = null!;

    public byte[] ConteudoCriptografado { get; set; } = Array.Empty<byte>();

    public byte[] Nonce { get; set; } = Array.Empty<byte>();

    public byte[] TagAutenticacao { get; set; } = Array.Empty<byte>();

    public byte[] SenhaCriptografada { get; set; } = Array.Empty<byte>();

    public byte[] SenhaNonce { get; set; } = Array.Empty<byte>();

    public byte[] SenhaTagAutenticacao { get; set; } = Array.Empty<byte>();

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
