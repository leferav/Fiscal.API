namespace Fiscal.API.Models.Database;

public class VinculacaoAgenteFiscal
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public string CodigoHash { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }

    public bool Utilizado { get; set; } = false;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? UtilizadoEm { get; set; }

    public Empresa Empresa { get; set; } = null!;
}