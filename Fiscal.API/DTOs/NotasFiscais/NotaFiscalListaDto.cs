namespace Fiscal.API.DTOs.NotasFiscais;

public class NotaFiscalListaDto
{
    public Guid Id { get; set; }

    public short Modelo { get; set; }

    public int Serie { get; set; }

    public long Numero { get; set; }

    public string? ChaveAcesso { get; set; }

    public short Ambiente { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? CStat { get; set; }

    public string? XMotivo { get; set; }

    public decimal ValorProdutos { get; set; }

    public decimal ValorTotal { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? AutorizadoEm { get; set; }
}