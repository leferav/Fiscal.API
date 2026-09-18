namespace Fiscal.API.Models.Requests.Cadastros
{
    public class CadastrarConfiguracaoTributariaRequest
    {
        public Guid EmpresaId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? CstIcms { get; set; }

        public string? Csosn { get; set; }

        public decimal AliquotaIcms { get; set; }

        public string Cfop { get; set; } = string.Empty;
    }
}