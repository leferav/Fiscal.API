namespace Fiscal.API.Models.Database;

public class ConfiguracaoFiscal
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    // 1 = Produção / 2 = Homologação
    public short Ambiente { get; set; } = 2;

    public int SerieNFe { get; set; } = 1;

    public int SerieNFCe { get; set; } = 1;

    public long ProximoNumeroNFe { get; set; } = 1;

    public long ProximoNumeroNFCe { get; set; } = 1;

    public string? Csc { get; set; }

    public string? CscId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Empresa Empresa { get; set; } = null!;
}