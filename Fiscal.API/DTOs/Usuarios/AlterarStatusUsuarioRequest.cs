
using System.ComponentModel.DataAnnotations;

namespace Fiscal.API.DTOs.Usuarios;

public class AlterarStatusUsuarioRequest
{
    [Required]
    public bool? Ativo { get; set; }

    // Somente o Master poderá informar a empresa.
    public Guid? EmpresaId { get; set; }
}

