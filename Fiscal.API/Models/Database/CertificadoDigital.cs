
namespace Fiscal.API.Models.Database;

public class CertificadoDigital
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmpresaId { get; set; }

    public Empresa Empresa { get; set; } = null!;

    // Campo obrigatório da estrutura anterior
    public int TipoArmazenamento { get; set; } = 1;

    // Identificação do certificado
    public string? NomeArquivo { get; set; }

    public string? Titular { get; set; }

    public string? Cnpj { get; set; }

    public string? Emissor { get; set; }

    public string? Thumbprint { get; set; }

    public string? NumeroSerie { get; set; }

    // Datas em UTC para PostgreSQL
    public DateTime? ValidoDe { get; set; }

    public DateTime? ValidoAte { get; set; }

    // Referência ao certificado armazenado de forma protegida
    public string? ReferenciaArmazenamento { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? AtualizadoEm { get; set; }
    public CertificadoDigitalArquivo? Arquivo { get; set; }
}
