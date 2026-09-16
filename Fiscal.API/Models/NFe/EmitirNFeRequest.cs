namespace Fiscal.API.Models.NFe
{
    public class EmitirNFeRequest
    {
        public DestinatarioRequest Destinatario { get; set; } = new();

        public List<ProdutoRequest> Produtos { get; set; } = new();

        public Guid EmpresaId { get; set; }
    }
}
