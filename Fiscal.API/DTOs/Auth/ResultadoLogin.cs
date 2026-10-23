
namespace Fiscal.API.DTOs.Auth;

public enum StatusLogin
{
    Sucesso,
    CredenciaisInvalidas,
    UsuarioInativo,
    MultiplasEmpresas
}

public class ResultadoLogin
{
    public StatusLogin Status { get; set; }

    public LoginResponse? Dados { get; set; }
}
