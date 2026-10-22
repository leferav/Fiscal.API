
using System.ComponentModel.DataAnnotations;

namespace Fiscal.API.DTOs.Usuarios;

public class CriarUsuarioRequest
{
    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Senha { get; set; } = string.Empty;

    [Required]
    public string Perfil { get; set; } = string.Empty;

    public Guid? EmpresaId { get; set; }
}
