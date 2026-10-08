
namespace Fiscal.API.Models.Database;

public class SolicitacaoEmissao
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmpresaId { get; set; }

    public Guid? AgenteId { get; set; }

    public Guid? NotaFiscalId { get; set; }

    public short Modelo { get; set; } = 65;

    public short Ambiente { get; set; }

    public int Serie { get; set; }

    public int Numero { get; set; }

    public string Status { get; set; } = "PENDENTE";

    // Conteúdo fiscal que será processado pelo Agent.
    // Não deve conter senha ou certificado PFX.
    public string PayloadJson { get; set; } = string.Empty;

    public string? ChaveAcesso { get; set; }

    public string? Protocolo { get; set; }

    public int? CStat { get; set; }

    public string? XMotivo { get; set; }

    public string? XmlAutorizado { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? IniciadoEm { get; set; }

    public DateTime? FinalizadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public string? Erro { get; set; }

    public Guid? TentativaId { get; set; }

    public DateTime? ReservaExpiraEm { get; set; }
}
