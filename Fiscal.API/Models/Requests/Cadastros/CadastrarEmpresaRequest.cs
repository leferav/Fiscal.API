namespace Fiscal.API.Models.Requests.Cadastros;

public class CadastrarEmpresaRequest
{
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string Uf { get; set; } = string.Empty;

    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;

    public int CodigoMunicipio { get; set; }
    public string Municipio { get; set; } = string.Empty;

    public string Cep { get; set; } = string.Empty;

    public int Crt { get; set; } = 1;
}