
using System.ComponentModel.DataAnnotations;

namespace Fiscal.API.Models.Requests.Cadastros;

public class CadastrarAdministradorRequest
{
    [Required(ErrorMessage = "Informe o nome.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;
}
