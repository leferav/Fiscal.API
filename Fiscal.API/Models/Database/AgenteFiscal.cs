namespace Fiscal.API.Models.Database;

public class AgenteFiscal
{
    public Guid Id { get; set; }

    public Guid EmpresaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? IdentificadorMaquina { get; set; }

    public string CredencialHash { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTime? UltimaComunicacaoEm { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? AtualizadoEm { get; set; }

    public Empresa Empresa { get; set; } = null!;

}