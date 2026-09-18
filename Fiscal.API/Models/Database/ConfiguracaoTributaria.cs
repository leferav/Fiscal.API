namespace Fiscal.API.Models.Database
{
    public class ConfiguracaoTributaria
    {
        public Guid Id { get; set; }

        public Guid EmpresaId { get; set; }

        public string Nome { get; set; } = string.Empty;

        // ICMS
        public string? CstIcms { get; set; }

        public string? Csosn { get; set; }

        public decimal AliquotaIcms { get; set; }

        // Operação
        public string Cfop { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime? AtualizadoEm { get; set; }

        public Empresa Empresa { get; set; } = null!;

        public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
    }
}