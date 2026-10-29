using System;
using System.Collections.Generic;

namespace Fiscal.API.Models.Database;

public class Venda
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public string Status { get; set; } = "A_ENVIAR";
    public string FormaPagamento { get; set; } = "01";
    public string? CpfConsumidor { get; set; }
    public decimal ValorTotal { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
    public Guid? NotaFiscalId { get; set; }

    public Empresa Empresa { get; set; } = null!;
    public NotaFiscal? NotaFiscal { get; set; }

    public Guid? ClienteId { get; set; }
    public ICollection<VendaItem> Itens { get; set; } = new List<VendaItem>();
}
