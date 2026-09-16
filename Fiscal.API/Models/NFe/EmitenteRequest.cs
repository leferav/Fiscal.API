namespace Fiscal.API.Models.NFe
{
    public class EmitenteRequest
    {
        public string Cnpj { get; set; } = string.Empty;
        public string InscricaoEstadual { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;

        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;

        public string CodigoMunicipio { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string Uf { get; set; } = "MG";
        public string Cep { get; set; } = string.Empty;

        public int Crt { get; set; }
    }
}
