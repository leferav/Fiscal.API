namespace Fiscal.API.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }

    public UsuarioLoginResponse Usuario { get; set; } = new();
    public EmpresaLoginResponse Empresa { get; set; } = new();
}

public class UsuarioLoginResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
}

public class EmpresaLoginResponse
{
    public Guid Id { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
}