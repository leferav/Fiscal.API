namespace Fiscal.API.Models.Database;

public class NotaFiscal
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    // 55 = NF-e / 65 = NFC-e
    public short Modelo { get; set; }

    public int Serie { get; set; }

    public long Numero { get; set; }

    public string? ChaveAcesso { get; set; }

    // 1 = Produção / 2 = Homologação
    public short Ambiente { get; set; }

    public string Status { get; set; } = "PENDENTE";

    public string? Protocolo { get; set; }

    public string? Recibo { get; set; }

    public int? CStat { get; set; }

    public string? XMotivo { get; set; }

    public decimal ValorProdutos { get; set; }

    public decimal ValorTotal { get; set; }

    public string? XmlEnvio { get; set; }

    public string? XmlRetorno { get; set; }

    public string? XmlAutorizado { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? AtualizadoEm { get; set; }

    public DateTime? AutorizadoEm { get; set; }

    public Empresa Empresa { get; set; } = null!;
}