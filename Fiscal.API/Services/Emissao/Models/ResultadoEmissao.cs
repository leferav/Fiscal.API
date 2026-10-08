namespace Fiscal.API.Services.Emissao.Models
{
    public class ResultadoEmissao
    {
        public bool Sucesso { get; set; }

        public int? CStat { get; set; }

        public string? Motivo { get; set; }

        public string? ChaveAcesso { get; set; }

        public string? Protocolo { get; set; }

        public string? XmlEnvio { get; set; }

        public string? XmlRetorno { get; set; }

        public string? XmlAutorizado { get; set; }
    }
}