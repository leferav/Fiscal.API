using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.NFe;
using Fiscal.API.Services.NFe;
using NFe.Classes.Informacoes.Identificacao;
using NFe.Classes.Informacoes.Identificacao.Tipos;

namespace Fiscal.API.Services.NFCe
{
    public class NFCeBuilder
    {
        private readonly EmitenteBuilder _emitenteBuilder;
        private readonly ProdutoBuilder _produtoBuilder;
        private readonly TotalBuilder _totalBuilder;
        private readonly TransporteBuilder _transporteBuilder;
        private readonly PagamentoBuilder _pagamentoBuilder;

        public NFCeBuilder(
            EmitenteBuilder emitenteBuilder,
            ProdutoBuilder produtoBuilder,
            TotalBuilder totalBuilder,
            TransporteBuilder transporteBuilder,
            PagamentoBuilder pagamentoBuilder)
        {
            _emitenteBuilder = emitenteBuilder;
            _produtoBuilder = produtoBuilder;
            _totalBuilder = totalBuilder;
            _transporteBuilder = transporteBuilder;
            _pagamentoBuilder = pagamentoBuilder;
        }

        // ============================================================
        // Cria a NFC-e completa
        // ============================================================
        public global::NFe.Classes.NFe Criar(
            List<ItemFiscal> itensFiscais,
            Empresa empresa,
            ConfiguracaoFiscal configuracaoFiscal)
        {
            var nfce = new global::NFe.Classes.NFe
            {
                infNFe = new global::NFe.Classes.Informacoes.infNFe
                {
                    versao = "4.00"
                }
            };

            // Identificação
            MontarIdentificacao(
                nfce,
                empresa,
                configuracaoFiscal
            );

            // Emitente
            nfce.infNFe.emit =
                _emitenteBuilder.Criar(empresa);

            // Produtos
            nfce.infNFe.det =
                _produtoBuilder.CriarNFCe(
                    itensFiscais,
                    empresa,
                    configuracaoFiscal
                );

            // Totais
            nfce.infNFe.total =
                _totalBuilder.Criar(
                    nfce.infNFe.det
                );

            // Transporte
            nfce.infNFe.transp =
                _transporteBuilder.Criar();

            // Pagamento
            nfce.infNFe.pag =
                _pagamentoBuilder.Criar(
                    nfce.infNFe.total.ICMSTot.vNF
                );

            return nfce;
        }

        // ============================================================
        // Identificação da NFC-e
        // ============================================================
        private void MontarIdentificacao(
            global::NFe.Classes.NFe nfce,
            Empresa empresa,
            ConfiguracaoFiscal configuracaoFiscal)
        {
            // ========================================================
            // UF da empresa
            // ========================================================

            var estado =
                Enum.Parse<Estado>(
                    empresa.Uf,
                    ignoreCase: true
                );

            // ========================================================
            // Ambiente
            //
            // 1 = Produção
            // 2 = Homologação
            // ========================================================

            var ambiente =
                configuracaoFiscal.Ambiente == 1
                    ? TipoAmbiente.Producao
                    : TipoAmbiente.Homologacao;

            // ========================================================
            // Numeração fiscal
            // ========================================================

            var numeroNFCe =
                configuracaoFiscal.ProximoNumeroNFCe;

            var serie =
                configuracaoFiscal.SerieNFCe;

            // ========================================================
            // IDE
            // ========================================================

            nfce.infNFe.ide = new ide
            {
                // UF
                cUF = estado,

                // Código numérico da chave
                cNF = Random.Shared
                    .Next(10000000, 99999999)
                    .ToString(),

                // Natureza da operação
                natOp = "VENDA",

                // Modelo 65 - NFC-e
                mod = ModeloDocumento.NFCe,

                // Série
                serie = serie,

                // Número vindo do banco
                nNF = numeroNFCe,

                // Data/hora
                dhEmi = DateTimeOffset.Now,

                // Saída
                tpNF = TipoNFe.tnSaida,

                // Operação interna
                idDest = DestinoOperacao.doInterna,

                // Município do fato gerador
                cMunFG = empresa.CodigoMunicipio,

                // DANFE NFC-e
                tpImp = TipoImpressao.tiNFCe,

                // Emissão normal
                tpEmis = TipoEmissao.teNormal,

                // Ambiente
                tpAmb = ambiente,

                // Finalidade normal
                finNFe = FinalidadeNFe.fnNormal,

                // Consumidor final
                indFinal =
                    ConsumidorFinal
                        .cfConsumidorFinal,

                // Operação presencial
                indPres =
                    PresencaComprador
                        .pcPresencial,

                // Aplicativo do contribuinte
                procEmi =
                    ProcessoEmissao
                        .peAplicativoContribuinte,

                // Versão do sistema
                verProc = "Fiscal.API 1.0"
            };
        }
    }
}