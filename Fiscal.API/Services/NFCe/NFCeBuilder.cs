using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.NFe;
using Fiscal.API.Services.NFe;
using NFe.Classes.Informacoes.Identificacao;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Utils;
using NFe.Utils.InformacoesSuplementares;

namespace Fiscal.API.Services.NFCe
{
    public class NFCeBuilder
    {
        private readonly EmitenteBuilder _emitenteBuilder;
        private readonly ProdutoBuilder _produtoBuilder;
        private readonly TotalBuilder _totalBuilder;
        private readonly TransporteBuilder _transporteBuilder;
        private readonly PagamentoBuilder _pagamentoBuilder;
        private readonly IConfiguration _configuration;

        public NFCeBuilder(
            EmitenteBuilder emitenteBuilder,
            ProdutoBuilder produtoBuilder,
            TotalBuilder totalBuilder,
            TransporteBuilder transporteBuilder,
            PagamentoBuilder pagamentoBuilder,
            IConfiguration configuration)
        {
            _emitenteBuilder = emitenteBuilder;
            _produtoBuilder = produtoBuilder;
            _totalBuilder = totalBuilder;
            _transporteBuilder = transporteBuilder;
            _pagamentoBuilder = pagamentoBuilder;
            _configuration = configuration;
        }

        public global::NFe.Classes.NFe Criar(EmitirNFeRequest request, Empresa empresa,ConfiguracaoFiscal configuracaoFiscal)
        {
            var nfce = new global::NFe.Classes.NFe
            {
                infNFe = new global::NFe.Classes.Informacoes.infNFe
                {
                    versao = "4.00"
                }
            };

            MontarIdentificacao(nfce, empresa, configuracaoFiscal);

            nfce.infNFe.emit = _emitenteBuilder.Criar(empresa);

            nfce.infNFe.det =
                _produtoBuilder.Criar(request.Produtos);

            nfce.infNFe.total =
                _totalBuilder.Criar(nfce.infNFe.det);

            nfce.infNFe.transp =
                _transporteBuilder.Criar();

            nfce.infNFe.pag =
                _pagamentoBuilder.Criar(
                    nfce.infNFe.total.ICMSTot.vNF
                );


            return nfce;
        }

        private void MontarIdentificacao(global::NFe.Classes.NFe nfce, Empresa empresa, ConfiguracaoFiscal configuracaoFiscal)
        {
            var estado = Enum.Parse<Estado>(
                empresa.Uf,
                ignoreCase: true);

            // Por enquanto continuaremos buscando o município da configuração.
            // Depois colocaremos endereço/município na tabela da empresa.
            var codigoMunicipio = int.Parse(
                _configuration["Fiscal:Emitente:CodigoMunicipio"]!
            );

            var ambiente =
                configuracaoFiscal.Ambiente == 1
                    ? TipoAmbiente.Producao
                    : TipoAmbiente.Homologacao;

            var numeroNFCe = configuracaoFiscal.ProximoNumeroNFCe;
            var serie = configuracaoFiscal.SerieNFCe;

            nfce.infNFe.ide = new ide
            {
                cUF = estado,

                cNF = Random.Shared
                    .Next(10000000, 99999999)
                    .ToString(),

                natOp = "VENDA",

                mod = ModeloDocumento.NFCe,

                serie = serie,
                nNF = numeroNFCe,

                dhEmi = DateTimeOffset.Now,

                tpNF = TipoNFe.tnSaida,

                idDest = DestinoOperacao.doInterna,

                cMunFG = codigoMunicipio,

                tpImp = TipoImpressao.tiNFCe,

                tpEmis = TipoEmissao.teNormal,

                tpAmb = ambiente,

                finNFe = FinalidadeNFe.fnNormal,

                indFinal = ConsumidorFinal.cfConsumidorFinal,

                indPres = PresencaComprador.pcPresencial,

                procEmi = ProcessoEmissao.peAplicativoContribuinte,

                verProc = "Fiscal.API 1.0"
            };
        }
    }
}