using Fiscal.API.Models.Enums;

namespace Fiscal.API.Models.Database
{
    public class CertificadoDigital
    {
        public Guid Id { get; set; }

        public Guid EmpresaId { get; set; }

        public TipoArmazenamentoCertificado TipoArmazenamento { get; set; } = TipoArmazenamentoCertificado.Local;

        public string? Titular { get; set; }

        public string? Cnpj { get; set; }

        public string? Emissor { get; set; }

        public string? Thumbprint { get; set; }

        public DateTime? ValidoDe { get; set; }

        public DateTime? ValidoAte { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public DateTime? AtualizadoEm { get; set; }

        public Empresa Empresa { get; set; } = null!;
    }
}