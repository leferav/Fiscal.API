
using System.ComponentModel.DataAnnotations;

namespace Fiscal.API.DTOs.Usuarios;

public class AtualizarUsuarioRequest
{
    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Perfil { get; set; } = string.Empty;

    // Somente o Master poderá informar a empresa.
    public Guid? EmpresaId { get; set; }
}
