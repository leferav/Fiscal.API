namespace Fiscal.API.Models.Database;

public class LogFiscal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid? NotaFiscalId { get; set; }
    public Guid? VendaId { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string Nivel { get; set; } = "Information";
    public string? Mensagem { get; set; }
    public string? DetalhesErro { get; set; }
    public string? ChaveAcesso { get; set; }
    public long? Numero { get; set; }
    public int? Serie { get; set; }
    public int? CStat { get; set; }
    public string? Protocolo { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
