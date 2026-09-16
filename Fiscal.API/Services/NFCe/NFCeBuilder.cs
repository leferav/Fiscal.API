using DFe.Classes.Entidades;
using DFe.Classes.Flags;
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

        public global::NFe.Classes.NFe Criar(EmitirNFeRequest request)
        {
            var nfce = new global::NFe.Classes.NFe
            {
                infNFe = new global::NFe.Classes.Informacoes.infNFe
                {
                    versao = "4.00"
                }
            };

            MontarIdentificacao(nfce);

            nfce.infNFe.emit = _emitenteBuilder.Criar();

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

        private void MontarIdentificacao(global::NFe.Classes.NFe nfce)
        {
            var ufTexto = _configuration["Fiscal:Uf"] ?? "MG";

            var estado = Enum.Parse<Estado>(
                ufTexto,
                ignoreCase: true);

            var codigoMunicipio = int.Parse(
                _configuration["Fiscal:Emitente:CodigoMunicipio"]!
            );

            var ambienteTexto =
                _configuration["Fiscal:Ambiente"];

            var ambiente =
                ambienteTexto == "Producao"
                    ? TipoAmbiente.Producao
                    : TipoAmbiente.Homologacao;

            var numeroNFCe = 900001;
            var serie = 1;

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