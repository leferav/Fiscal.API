namespace Fiscal.API.Models.Requests.Cadastros
{
    public class CadastrarProdutoRequest
    {
        public Guid EmpresaId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string Ncm { get; set; } = string.Empty;

        public string Unidade { get; set; } = "UN";
        public decimal ValorVenda { get; set; }

        public Guid ConfiguracaoTributariaId { get; set; }
    }
}