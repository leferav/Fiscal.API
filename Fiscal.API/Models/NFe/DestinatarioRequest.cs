public class DestinatarioRequest
{
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? InscricaoEstadual { get; set; }

    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string CodigoMunicipio { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Uf { get; set; } = "MG";
    public string Cep { get; set; } = string.Empty;
    public string? Email { get; set; }
}