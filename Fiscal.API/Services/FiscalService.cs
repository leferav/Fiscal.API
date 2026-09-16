using DFe.Classes.Flags;
using Fiscal.API.Models.NFe;
using Fiscal.API.Services.NFCe;
using Fiscal.API.Services.NFe;
using NFe.Classes.Servicos.Tipos;
using NFe.Servicos;
using NFe.Utils;
using NFe.Utils.InformacoesSuplementares;
using NFe.Utils.NFe;

namespace Fiscal.API.Services
{
    public class FiscalService
    {
        private readonly ZeusConfigurationFactory _configFactory;
        private readonly NFeBuilder _nfeBuilder;
        private readonly NFCeBuilder _nfceBuilder;
        private readonly NFeXmlService _nfeXmlService;
        private readonly IConfiguration _configuration;

        public FiscalService(
            ZeusConfigurationFactory configFactory,
            NFeBuilder nfeBuilder,
            NFeXmlService nfeXmlService,
            NFCeBuilder nfceBuilder,
            IConfiguration configuration)
        {
            _configFactory = configFactory;
            _nfeBuilder = nfeBuilder;
            _nfeXmlService = nfeXmlService;
            _nfceBuilder = nfceBuilder;
            _configuration = configuration; 
        }

        #region NF-e
            public object ConsultarStatusNFe()
            {
                var configuracao = _configFactory.Criar();

                using var servicoNFe = new ServicosNFe(configuracao);

                var retorno = servicoNFe.NfeStatusServico();

                return new
                {
                    XmlEnvio = retorno.EnvioStr,
                    XmlRetorno = retorno.RetornoStr
                };
            }

            public object GerarNFe(EmitirNFeRequest request)
            {
                var nfe = _nfeBuilder.Criar(request);

                var xml = _nfeXmlService.AssinarXml(nfe);

                return new
                {
                    mensagem = "XML da NF-e gerado e assinado com sucesso.",
                    versao = nfe.infNFe.versao,
                    numero = nfe.infNFe.ide.nNF,
                    serie = nfe.infNFe.ide.serie,
                    id = nfe.infNFe.Id,
                    xml
                };
            }

            public object AutorizarNFe(EmitirNFeRequest request)
            {
                // Monta a NF-e
                var nfe = _nfeBuilder.Criar(request);

                // Obtém a mesma configuração usada no restante do projeto
                var configuracao = _configFactory.Criar();

                // Assina com o certificado A1
                nfe.Assina(configuracao);

                // Valida contra os schemas
                nfe.Valida(configuracao);

                // Cria o serviço do Zeus
                using var servicoNFe = new ServicosNFe(configuracao);

                // Lote de teste
                var idLote = 1;

                var retorno = servicoNFe.NFeAutorizacao(
                    idLote,
                    IndicadorSincronizacao.Sincrono,
                    new List<global::NFe.Classes.NFe>
                    {
                nfe
                    },
                    false
                );

                var retEnviNFe = retorno.Retorno;

                var protocolo = retEnviNFe.protNFe;

                return new
                {
                    sucesso = protocolo?.infProt?.cStat == 100,

                    ambiente = configuracao.tpAmb.ToString(),

                    chave = protocolo?.infProt?.chNFe
                            ?? nfe.infNFe.Id?.Replace("NFe", ""),

                    cStatLote = retEnviNFe.cStat,
                    motivoLote = retEnviNFe.xMotivo,

                    cStat = protocolo?.infProt?.cStat,
                    motivo = protocolo?.infProt?.xMotivo,

                    protocolo = protocolo?.infProt?.nProt
                };
            }

            public object ConsultarNFe(string chave)
            {
                if (string.IsNullOrWhiteSpace(chave))
                    throw new Exception("Chave da NF-e não informada.");

                chave = new string(chave.Where(char.IsDigit).ToArray());

                if (chave.Length != 44)
                    throw new Exception("A chave da NF-e deve possuir 44 números.");

                var configuracao = _configFactory.Criar();

                using var servicoNFe = new ServicosNFe(configuracao);

                var retorno = servicoNFe.NfeConsultaProtocolo(chave);

                var retConsSitNFe = retorno.Retorno;

                return new
                {
                    chave,
                    ambiente = configuracao.tpAmb.ToString(),
                    cStat = retConsSitNFe.cStat,
                    motivo = retConsSitNFe.xMotivo,
                    protocolo = retConsSitNFe.protNFe?.infProt?.nProt
                };
            }

        #endregion



        #region NFC-e

        public object AutorizarNFCe(EmitirNFeRequest request)
        {
            var nfce = _nfceBuilder.Criar(request);

            var configuracao = _configFactory.Criar();

            configuracao.ModeloDocumento = ModeloDocumento.NFCe;

            // Primeiro assina
            nfce.Assina(configuracao);

            // Depois gera informações suplementares da NFC-e
            var idCsc = _configuration["Fiscal:Csc:Id"];
            var csc = _configuration["Fiscal:Csc:Token"];

            nfce.infNFeSupl = new global::NFe.Classes.infNFeSupl();
            nfce.infNFeSupl.urlChave =
                nfce.infNFeSupl.ObterUrlConsulta(
                    nfce,
                    VersaoQrCode.QrCodeVersao3);

            nfce.infNFeSupl.qrCode =
                nfce.infNFeSupl.ObterUrlQrCode(
                    nfce,
                    VersaoQrCode.QrCodeVersao3,
                    idCsc,
                    csc,
                    configuracao.Certificado);


            // Valida o XML completo
            nfce.Valida(configuracao);

            // ============================================
            // DEBUG - XML da NFC-e antes de enviar à SEFAZ
            // ============================================
            var xmlNfce = nfce.ObterXmlString();

            Console.WriteLine("========== XML NFC-e ==========");
            Console.WriteLine(xmlNfce);
            Console.WriteLine("================================");


            using var servicoNFe = new ServicosNFe(configuracao);
            Console.WriteLine($"UF configurada: {configuracao.cUF}");
            Console.WriteLine($"Ambiente: {configuracao.tpAmb}");

            var idLote = 1;

            var retorno = servicoNFe.NFeAutorizacao(
                idLote,
                IndicadorSincronizacao.Sincrono,
                new List<global::NFe.Classes.NFe>
                {
            nfce
                },
                false
            );

            var retEnviNFe = retorno.Retorno;
            var protocolo = retEnviNFe.protNFe;

            return new
            {
                sucesso = protocolo?.infProt?.cStat == 100,

                ambiente = configuracao.tpAmb.ToString(),

                modelo = "NFC-e 65",

                chave = protocolo?.infProt?.chNFe
                        ?? nfce.infNFe.Id?.Replace("NFe", ""),

                cStatLote = retEnviNFe.cStat,

                motivoLote = retEnviNFe.xMotivo,

                cStat = protocolo?.infProt?.cStat,

                motivo = protocolo?.infProt?.xMotivo,

                protocolo = protocolo?.infProt?.nProt
            };
        }

        public object ConsultarNFCe(string chave)
        {
            if (string.IsNullOrWhiteSpace(chave))
                throw new Exception("Chave da NFC-e não informada.");

            chave = new string(chave.Where(char.IsDigit).ToArray());

            if (chave.Length != 44)
                throw new Exception("A chave da NFC-e deve possuir 44 números.");

            var configuracao = _configFactory.Criar();

            // Importante: modelo 65
            configuracao.ModeloDocumento = ModeloDocumento.NFCe;

            using var servicoNFe = new ServicosNFe(configuracao);

            var retorno = servicoNFe.NfeConsultaProtocolo(chave);

            var retConsSitNFe = retorno.Retorno;

            return new
            {
                chave,

                ambiente = configuracao.tpAmb.ToString(),

                modelo = "NFC-e 65",

                cStat = retConsSitNFe.cStat,

                motivo = retConsSitNFe.xMotivo,

                protocolo = retConsSitNFe.protNFe?.infProt?.nProt
            };
        }

        #endregion

    }
}