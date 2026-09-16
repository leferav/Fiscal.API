using NFe.Utils;
using NFe.Utils.NFe;

namespace Fiscal.API.Services.NFe
{
    public class NFeXmlService
    {
        private readonly ZeusConfigurationFactory _configFactory;

        public NFeXmlService(ZeusConfigurationFactory configFactory)
        {
            _configFactory = configFactory;
        }

        public string GerarXml(global::NFe.Classes.NFe nfe)
        {
            return nfe.ObterXmlString();
        }

        public string AssinarXml(global::NFe.Classes.NFe nfe)
        {
            var configuracao = _configFactory.Criar();

            // Gera chave/Id e assina com o certificado A1
            nfe.Assina(configuracao);

            // Valida a NF-e utilizando os schemas XSD
            nfe.Valida(configuracao);

            return nfe.ObterXmlString();
        }
    }
}