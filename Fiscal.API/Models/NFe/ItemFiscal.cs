using Fiscal.API.Models.Database;

namespace Fiscal.API.Models.NFe
{
    public class ItemFiscal
    {
        public Guid ProdutoId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string Ncm { get; set; } = string.Empty;

        public string Unidade { get; set; } = "UN";

        public decimal Quantidade { get; set; }

        public decimal ValorUnitario { get; set; }

        public ConfiguracaoTributaria ConfiguracaoTributaria { get; set; }
            = null!;
    }
}