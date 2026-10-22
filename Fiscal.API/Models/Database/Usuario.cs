using Fiscal.API.Models.Database;

namespace Fiscal.API.Models;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Campos atuais: manter durante a migração
    public Guid EmpresaId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string SenhaHash { get; set; } = string.Empty;

    public string Perfil { get; set; } = "Operador";

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Empresa Empresa { get; set; } = null!;

    // Novo campo: administrador geral do Fiscal.API
    public bool AdministradorGeral { get; set; } = false;

    // Novos vínculos com as empresas
    public ICollection<UsuarioEmpresa> UsuariosEmpresas
    { get; set; } = new List<UsuarioEmpresa>();
}