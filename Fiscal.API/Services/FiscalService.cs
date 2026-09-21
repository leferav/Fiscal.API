using DFe.Classes.Flags;
using Fiscal.API.Data;
using Fiscal.API.Models.Database;
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
        private readonly FiscalDbContext _context;

        public FiscalService(
            ZeusConfigurationFactory configFactory,
            NFeBuilder nfeBuilder,
            NFeXmlService nfeXmlService,
            NFCeBuilder nfceBuilder,
            FiscalDbContext context)
        {
            _configFactory = configFactory;
            _nfeBuilder = nfeBuilder;
            _nfeXmlService = nfeXmlService;
            _nfceBuilder = nfceBuilder;
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

        public async Task<object> AutorizarNFCeAsync(EmitirNFeRequest request)
        {
            var empresa = await _context.Empresas
                .Include(x => x.ConfiguracaoFiscal)
                .Include(x => x.ConfiguracoesTributarias)
                .FirstOrDefaultAsync(x => x.Id == request.EmpresaId);

            if (empresa is null)
                throw new Exception("Empresa não encontrada.");

            if (!empresa.Ativo)
                throw new Exception("Empresa está inativa.");

            if (empresa.ConfiguracaoFiscal is null)
                throw new Exception(
                    "Empresa não possui configuração fiscal.");


            var configuracaoFiscal = empresa.ConfiguracaoFiscal;

            if (request.Produtos == null || request.Produtos.Count == 0)
            {
                throw new Exception(
                    "Informe pelo menos um produto."
                );
            }

            // IDs dos produtos recebidos pelo PDV
            var produtoIds = request.Produtos
                    .Select(x => x.ProdutoId)
                    .Distinct()
                    .ToList();

            // Carrega produtos + configuração tributária
            var produtosCadastrados =
                await _context.Produtos
                    .AsNoTracking()
                    .Include(x => x.ConfiguracaoTributaria)
                    .Where(x =>
                        produtoIds.Contains(x.Id) &&
                        x.EmpresaId == empresa.Id &&
                        x.Ativo)
                    .ToListAsync();

            // Monta os itens fiscais internos
            var itensFiscais = new List<ItemFiscal>();

            foreach (var itemRequest in request.Produtos)
            {
                if (itemRequest.ProdutoId == Guid.Empty)
                {
                    throw new Exception(
                        "ProdutoId não informado."
                    );
                }

                if (itemRequest.Quantidade <= 0)
                {
                    throw new Exception(
                        "A quantidade deve ser maior que zero."
                    );
                }

                var produto = produtosCadastrados.FirstOrDefault(x => x.Id == itemRequest.ProdutoId);

                if (produto == null)
                {
                    throw new Exception(
                        $"Produto {itemRequest.ProdutoId} não encontrado, " +
                        $"inativo ou pertencente a outra empresa."
                    );
                }

                if (produto.ValorVenda <= 0)
                {
                    throw new Exception(
                        $"O produto {produto.Codigo} não possui valor de venda válido."
                    );
                }


                if (produto.ConfiguracaoTributaria == null)
                {
                    throw new Exception(
                        $"Produto {produto.Codigo} não possui " +
                        $"configuração tributária."
                    );
                }

                if (!produto.ConfiguracaoTributaria.Ativo)
                {
                    throw new Exception(
                        $"A configuração tributária do produto " +
                        $"{produto.Codigo} está inativa."
                    );
                }

                itensFiscais.Add(
                    new ItemFiscal
                    {
                        ProdutoId = produto.Id,
                        Codigo = produto.Codigo,
                        Descricao = produto.Descricao,
                        Ncm = produto.Ncm,
                        Unidade = produto.Unidade,
                        Quantidade = itemRequest.Quantidade,
                        ValorUnitario = produto.ValorVenda,
                        ConfiguracaoTributaria = produto.ConfiguracaoTributaria
                    }
                );
            }

            // Monta a NFC-e
            var nfce =
                _nfceBuilder.Criar(
                    itensFiscais,
                    empresa,
                    configuracaoFiscal,
                    request.Destinatario
                );


            var configuracao = _configFactory.Criar();

            configuracao.ModeloDocumento = ModeloDocumento.NFCe;

            // Primeiro assina
            nfce.Assina(configuracao);

            // Depois gera informações suplementares da NFC-e
            var idCsc = configuracaoFiscal.CscId;
            var csc = configuracaoFiscal.Csc;

            nfce.infNFeSupl = new global::NFe.Classes.infNFeSupl();
            nfce.infNFeSupl.urlChave = nfce.infNFeSupl.ObterUrlConsulta(nfce, VersaoQrCode.QrCodeVersao3);
            nfce.infNFeSupl.qrCode = nfce.infNFeSupl.ObterUrlQrCode(nfce, VersaoQrCode.QrCodeVersao3, idCsc, csc, configuracao.Certificado);

            // Valida o XML completo
            nfce.Valida(configuracao);

            var xmlNfce = nfce.ObterXmlString();

            using var servicoNFe = new ServicosNFe(configuracao);

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

            var autorizada = protocolo?.infProt?.cStat == 100;

            string? xmlAutorizado = null;

            if (autorizada && protocolo != null)
            {
                var proc = new global::NFe.Classes.nfeProc
                {
                    versao = "4.00",
                    NFe = nfce,
                    protNFe = protocolo
                };

                xmlAutorizado = proc.ObterXmlString();
            }

            var notaFiscal = new NotaFiscal
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresa.Id,
                Modelo = 65,
                Serie = nfce.infNFe.ide.serie,
                Numero = nfce.infNFe.ide.nNF,
                ChaveAcesso = protocolo?.infProt?.chNFe ?? nfce.infNFe.Id?.Replace("NFe", ""),
                Ambiente = (short)configuracaoFiscal.Ambiente,
                Status = autorizada
                    ? "AUTORIZADA"
                    : "REJEITADA",
                Protocolo = protocolo?.infProt?.nProt,
                CStat = protocolo?.infProt?.cStat
                        ?? retEnviNFe.cStat,
                XMotivo = protocolo?.infProt?.xMotivo
                          ?? retEnviNFe.xMotivo,
                ValorProdutos = nfce.infNFe.total.ICMSTot.vProd,
                ValorTotal = nfce.infNFe.total.ICMSTot.vNF,
                XmlEnvio = xmlNfce,
                XmlRetorno = retorno.RetornoStr,
                XmlAutorizado = xmlAutorizado,
                CriadoEm = DateTime.UtcNow,
                AutorizadoEm = autorizada
                    ? DateTime.UtcNow
                    : null
            };

            _context.NotasFiscais.Add(notaFiscal);

            if (autorizada)
            {
                configuracaoFiscal.ProximoNumeroNFCe++;
            }

            await _context.SaveChangesAsync();

            return new
            {
                sucesso = autorizada,
                ambiente = configuracao.tpAmb.ToString(),
                modelo = "NFC-e 65",
                chave = protocolo?.infProt?.chNFe ?? nfce.infNFe.Id?.Replace("NFe", ""),
                cStatLote = retEnviNFe.cStat,
                motivoLote = retEnviNFe.xMotivo,
                cStat = protocolo?.infProt?.cStat,
                motivo = protocolo?.infProt?.xMotivo,
                protocolo = protocolo?.infProt?.nProt,
                numero = nfce.infNFe.ide.nNF,
                serie = nfce.infNFe.ide.serie
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