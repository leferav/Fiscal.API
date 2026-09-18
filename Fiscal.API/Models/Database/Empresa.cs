namespace Fiscal.API.Models.Database;

public class Empresa
{
    public Guid Id { get; set; }

    public string Cnpj { get; set; } = string.Empty;

    public string RazaoSocial { get; set; } = string.Empty;

    public string? NomeFantasia { get; set; }

    public string? InscricaoEstadual { get; set; }

    public string Uf { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;

    public int CodigoMunicipio { get; set; }
    public string Municipio { get; set; } = string.Empty;

    public string Cep { get; set; } = string.Empty;

    public int Crt { get; set; } = 1;

    public ConfiguracaoFiscal? ConfiguracaoFiscal { get; set; }

    public ICollection<NotaFiscal> NotasFiscais { get; set; }
        = new List<NotaFiscal>();

    public ICollection<ConfiguracaoTributaria> ConfiguracoesTributarias { get; set; }
    = new List<ConfiguracaoTributaria>();

    public ICollection<Produto> Produtos { get; set; }
    = new List<Produto>();

}