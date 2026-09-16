using DFe.Classes.Flags;
using Fiscal.API.Data;
using Fiscal.API.Models.NFe;
using Fiscal.API.Services.NFCe;
using Fiscal.API.Services.NFe;
using Microsoft.EntityFrameworkCore;
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
        private readonly FiscalDbContext _context;

        public FiscalService(
            ZeusConfigurationFactory configFactory,
            NFeBuilder nfeBuilder,
            NFeXmlService nfeXmlService,
            NFCeBuilder nfceBuilder,
            IConfiguration configuration,
            FiscalDbContext context )
        {
            _configFactory = configFactory;
            _nfeBuilder = nfeBuilder;
            _nfeXmlService = nfeXmlService;
            _nfceBuilder = nfceBuilder;
            _configuration = configuration; 
            _context = context;
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

        public async Task<object> AutorizarNFCeAsync(
            EmitirNFeRequest request)
        {
            var empresa = await _context.Empresas
                .AsNoTracking()
                .Include(x => x.ConfiguracaoFiscal)
                .FirstOrDefaultAsync(x => x.Id == request.EmpresaId);

            if (empresa is null)
                throw new Exception("Empresa não encontrada.");

            if (!empresa.Ativo)
                throw new Exception("Empresa está inativa.");

            if (empresa.ConfiguracaoFiscal is null)
                throw new Exception(
                    "Empresa não possui configuração fiscal.");

            var configuracaoFiscal = empresa.ConfiguracaoFiscal;

            var nfce = _nfceBuilder.Criar(
                                            request,
                                            empresa,
                                            configuracaoFiscal);

            var configuracao = _configFactory.Criar();

            configuracao.ModeloDocumento = ModeloDocumento.NFCe;

            // Primeiro assina
            nfce.Assina(configuracao);

            // Depois gera informações suplementares da NFC-e
            var idCsc = configuracaoFiscal.CscId;
            var csc = configuracaoFiscal.Csc;

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



            Console.WriteLine("========== IDE ==========");

            Console.WriteLine($"Modelo: {nfce.infNFe?.ide?.mod}");
            Console.WriteLine($"Serie: {nfce.infNFe?.ide?.serie}");
            Console.WriteLine($"Numero: {nfce.infNFe?.ide?.nNF}");
            Console.WriteLine($"UF: {nfce.infNFe?.ide?.cUF}");
            Console.WriteLine($"Ambiente: {nfce.infNFe?.ide?.tpAmb}");

            Console.WriteLine("=========================");

            Console.WriteLine("========== EMITENTE ==========");

            Console.WriteLine($"CNPJ: {nfce.infNFe?.emit?.CNPJ}");
            Console.WriteLine($"Razao Social: {nfce.infNFe?.emit?.xNome}");
            Console.WriteLine($"IE: {nfce.infNFe?.emit?.IE}");

            Console.WriteLine("==============================");


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