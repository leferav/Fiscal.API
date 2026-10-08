namespace Fiscal.Agent.Models;

public class ConfiguracaoLocal
{
    public string CaminhoCertificado { get; set; } = string.Empty;

    public string SenhaProtegida { get; set; } = string.Empty;

    public Guid? AgenteId { get; set; }

    public string CredencialAgenteProtegida { get; set; } = string.Empty;
}