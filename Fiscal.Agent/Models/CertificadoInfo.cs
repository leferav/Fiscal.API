namespace Fiscal.Agent.Models;

public class CertificadoInfo
{
    public string? Titular { get; set; }

    public string? Cnpj { get; set; }

    public string? Emissor { get; set; }

    public string? Thumbprint { get; set; }

    public DateTime ValidoDe { get; set; }

    public DateTime ValidoAte { get; set; }

    public bool PossuiChavePrivada { get; set; }
}