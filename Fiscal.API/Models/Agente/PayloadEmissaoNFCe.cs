
namespace Fiscal.API.Models.Agente;

public class PayloadEmissaoNFCe
{
    public int VersaoContrato { get; set; } = 1;

    public Guid EmpresaId { get; set; }

    public short Modelo { get; set; } = 65;

    public short Ambiente { get; set; }

    public int Serie { get; set; }

    public int Numero { get; set; }

    public string XmlSemAssinatura { get; set; } = string.Empty;
}
