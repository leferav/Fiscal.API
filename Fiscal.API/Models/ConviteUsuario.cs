
namespace Fiscal.API.Models;

public class ConviteUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmpresaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = "Administrador";

    // Armazena somente o hash do token do convite.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }

    public DateTime? UtilizadoEm { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
