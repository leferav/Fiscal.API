
namespace Fiscal.API.DTOs.Common;

public class ErroResponse
{
    public string Codigo { get; set; } = string.Empty;

    public string Mensagem { get; set; } = string.Empty;

    public string? TraceId { get; set; }
}
