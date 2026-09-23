using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.NFe;
using Fiscal.API.Services.NFe;
using NFe.Classes.Informacoes.Destinatario;
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
            ConfiguracaoFiscal configuracaoFiscal,
            DestinatarioRequest? destinatario = null)
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

            // Destinatário / CPF na nota
            MontarDestinatario(
                nfce,
                destinatario
            );

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
        // Destinatário da NFC-e
        // CPF na nota é opcional
        // ============================================================
        private void MontarDestinatario(
            global::NFe.Classes.NFe nfce,
            DestinatarioRequest? destinatario)
        {
            if (destinatario == null ||
                string.IsNullOrWhiteSpace(destinatario.CpfCnpj))
            {
                return;
            }

            // Remove pontos, traços, barras etc.
            var documento = new string(
                destinatario.CpfCnpj
                    .Where(char.IsDigit)
                    .ToArray()
            );

            // valida CPF.
            if (!CpfValido(documento))
            {
                throw new Exception(
                    "O CPF informado é inválido."
                );
            }

            nfce.infNFe.dest = new dest(
                VersaoServico.Versao400
            )
            {
                CPF = documento,

                indIEDest =
                    indIEDest.NaoContribuinte
            };

            // Nome é opcional para nosso fluxo atual.
            if (!string.IsNullOrWhiteSpace(destinatario.Nome))
            {
                nfce.infNFe.dest.xNome =
                    destinatario.Nome.Trim();
            }
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



            // IDE
            var fusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var dataHoraEmissao = TimeZoneInfo.ConvertTime(
                DateTimeOffset.UtcNow,
                fusoBrasil
            );
            nfce.infNFe.ide = new ide
            {
                cUF = estado,
                cNF = Random.Shared.Next(10000000, 99999999).ToString(),
                natOp = "VENDA",
                mod = ModeloDocumento.NFCe,
                serie = serie,
                nNF = numeroNFCe,
                dhEmi = dataHoraEmissao,
                tpNF = TipoNFe.tnSaida,
                idDest = DestinoOperacao.doInterna,
                cMunFG = empresa.CodigoMunicipio,
                tpImp = TipoImpressao.tiNFCe,
                tpEmis = TipoEmissao.teNormal,
                tpAmb = ambiente,
                finNFe = FinalidadeNFe.fnNormal,
                indFinal = ConsumidorFinal.cfConsumidorFinal,
                indPres =PresencaComprador.pcPresencial,
                procEmi = ProcessoEmissao.peAplicativoContribuinte,
                verProc = "Fiscal.API 1.0"
            };
        }


        private static bool CpfValido(string cpf)
        {
            cpf = new string(
                cpf.Where(char.IsDigit).ToArray()
            );

            if (cpf.Length != 11)
                return false;

            if (cpf.Distinct().Count() == 1)
                return false;

            var numeros = cpf
                .Select(c => c - '0')
                .ToArray();

            var soma = 0;

            for (var i = 0; i < 9; i++)
                soma += numeros[i] * (10 - i);

            var resto = soma % 11;

            var digito1 =
                resto < 2
                    ? 0
                    : 11 - resto;

            if (numeros[9] != digito1)
                return false;

            soma = 0;

            for (var i = 0; i < 10; i++)
                soma += numeros[i] * (11 - i);

            resto = soma % 11;

            var digito2 =
                resto < 2
                    ? 0
                    : 11 - resto;

            return numeros[10] == digito2;
        }
    }
}