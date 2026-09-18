namespace Fiscal.API.Models.Database
{
    public class Produto
    {
        public Guid Id { get; set; }

        public Guid EmpresaId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string Ncm { get; set; } = string.Empty;

        public string Unidade { get; set; } = "UN";

        public Guid ConfiguracaoTributariaId { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime? AtualizadoEm { get; set; }

        public Empresa Empresa { get; set; } = null!;

        public decimal ValorVenda { get; set; }

        public ConfiguracaoTributaria ConfiguracaoTributaria { get; set; } = null!;
    }
}