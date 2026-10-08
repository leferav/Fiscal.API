namespace Fiscal.API.Services.Emissao.Models
{
    public class SolicitacaoEmissao
    {
        public Guid EmpresaId { get; set; }

        public int Modelo { get; set; }

        public int Serie { get; set; }

        public int Numero { get; set; }

        public string Xml { get; set; } = string.Empty;

        public string? CscId { get; set; }

        public string? Csc { get; set; }
    }
}