using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Utils;

namespace Fiscal.API.Services.NFe
{
    public class ZeusConfigurationFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public ZeusConfigurationFactory(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public ConfiguracaoServico Criar()
        {
            var certificadoRelativo =
                _configuration["Fiscal:Certificado"];

            var senhaCertificado =
                _configuration["Fiscal:SenhaCertificado"];

            if (string.IsNullOrWhiteSpace(certificadoRelativo))
                throw new Exception("Certificado não configurado.");

            if (string.IsNullOrWhiteSpace(senhaCertificado))
                throw new Exception("Senha do certificado não configurada.");

            var caminhoCertificado = Path.Combine(
                _environment.ContentRootPath,
                certificadoRelativo
            );

            if (!File.Exists(caminhoCertificado))
                throw new Exception(
                    $"Certificado não encontrado: {caminhoCertificado}"
                );

            var caminhoSchemas = Path.Combine(
                _environment.ContentRootPath,
                "Schemas"
            );

            if (!Directory.Exists(caminhoSchemas))
                throw new Exception(
                    $"Diretório de schemas não encontrado: {caminhoSchemas}"
                );

            return new ConfiguracaoServico
            {
                cUF = Estado.GO,
                tpAmb = TipoAmbiente.Homologacao,
                VersaoLayout = VersaoServico.Versao400,
                ModeloDocumento = ModeloDocumento.NFe,
                tpEmis = TipoEmissao.teNormal,

                TimeOut = 30000,

                DefineVersaoServicosAutomaticamente = true,

                ValidarSchemas = true,
                DiretorioSchemas = caminhoSchemas,

                Certificado = new ConfiguracaoCertificado
                {
                    TipoCertificado = TipoCertificado.A1Arquivo,
                    Arquivo = caminhoCertificado,
                    Senha = senhaCertificado
                }
            };
        }
    }
}
