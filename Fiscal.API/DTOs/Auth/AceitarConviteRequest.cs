
using System.ComponentModel.DataAnnotations;

namespace Fiscal.API.DTOs.Auth;

public class AceitarConviteRequest
{
    [Required(ErrorMessage = "Informe o token do convite.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [MinLength(12, ErrorMessage =
        "A senha deve conter pelo menos 12 caracteres.")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
