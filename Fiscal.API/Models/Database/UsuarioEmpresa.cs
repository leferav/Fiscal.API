
using Fiscal.API.Models.Database;

namespace Fiscal.API.Models;

public class UsuarioEmpresa
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsuarioId { get; set; }

    public Guid EmpresaId { get; set; }

    public string Perfil { get; set; } = "Operador";

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Usuario Usuario { get; set; } = null!;

    public Empresa Empresa { get; set; } = null!;
}
