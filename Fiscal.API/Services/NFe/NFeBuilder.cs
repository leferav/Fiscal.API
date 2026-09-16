using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using Fiscal.API.Models.NFe;
using NFe.Classes.Informacoes.Identificacao;
using NFe.Classes.Informacoes.Identificacao.Tipos;

namespace Fiscal.API.Services.NFe
{
    public class NFeBuilder
    {
        private readonly EmitenteBuilder _emitenteBuilder;
        private readonly DestinatarioBuilder _destinatarioBuilder;
        private readonly ProdutoBuilder _produtoBuilder;
        private readonly TotalBuilder _totalBuilder;
        private readonly TransporteBuilder _transporteBuilder;
        private readonly PagamentoBuilder _pagamentoBuilder;

        public NFeBuilder(
            EmitenteBuilder emitenteBuilder,
            DestinatarioBuilder destinatarioBuilder,
            ProdutoBuilder produtoBuilder,
            TotalBuilder totalBuilder,
            TransporteBuilder transporteBuilder,
            PagamentoBuilder pagamentoBuilder)
        {
            _emitenteBuilder = emitenteBuilder;
            _destinatarioBuilder = destinatarioBuilder;
            _produtoBuilder = produtoBuilder;
            _totalBuilder = totalBuilder;
            _transporteBuilder = transporteBuilder;
            _pagamentoBuilder = pagamentoBuilder;
        }

        public global::NFe.Classes.NFe Criar(EmitirNFeRequest request)
        {
            var nfe = new global::NFe.Classes.NFe
            {
                infNFe = new global::NFe.Classes.Informacoes.infNFe
                {
                    versao = "4.00"
                }
            };

            MontarIdentificacao(nfe, request);

            nfe.infNFe.emit = _emitenteBuilder.Criar();
            nfe.infNFe.dest = _destinatarioBuilder.Criar(request.Destinatario);
            nfe.infNFe.det = _produtoBuilder.Criar(request.Produtos);
            nfe.infNFe.total = _totalBuilder.Criar(nfe.infNFe.det);
            nfe.infNFe.transp = _transporteBuilder.Criar();
            nfe.infNFe.pag = _pagamentoBuilder.Criar(nfe.infNFe.total.ICMSTot.vNF);
            return nfe;
        }

        private void MontarIdentificacao(
            global::NFe.Classes.NFe nfe,
            EmitirNFeRequest request)
        {
            var numeroNFe = 2;
            var serie = 1;

            var ufDestinatario = request.Destinatario.Uf
                ?.Trim()
                .ToUpperInvariant();

            var destinoOperacao =
                ufDestinatario == "MG"
                    ? DestinoOperacao.doInterna
                    : DestinoOperacao.doInterestadual;

            nfe.infNFe.ide = new ide
            {
                cUF = Estado.MG,

                cNF = Random.Shared
                    .Next(10000000, 99999999)
                    .ToString(),

                natOp = "VENDA",
                mod = ModeloDocumento.NFe,
                serie = serie,
                nNF = numeroNFe,
                dhEmi = DateTimeOffset.Now,
                dhSaiEnt = DateTimeOffset.Now,
                tpNF = TipoNFe.tnSaida,

                idDest = destinoOperacao,

                cMunFG = 3103504,
                tpImp = TipoImpressao.tiRetrato,
                tpEmis = TipoEmissao.teNormal,
                tpAmb = TipoAmbiente.Homologacao,
                finNFe = FinalidadeNFe.fnNormal,
                indFinal = ConsumidorFinal.cfConsumidorFinal,
                indPres = PresencaComprador.pcPresencial,
                procEmi = ProcessoEmissao.peAplicativoContribuinte,
                verProc = "Fiscal.API 1.0"
            };
        }
    }
}