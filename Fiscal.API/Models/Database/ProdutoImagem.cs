
namespace Fiscal.API.Models.Database
{
    public class ProdutoImagem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProdutoId { get; set; }

        public byte[] Imagem { get; set; } = Array.Empty<byte>();

        public string ContentType { get; set; } = "image/webp";

        public int TamanhoBytes { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public Produto Produto { get; set; } = null!;
    }
}
