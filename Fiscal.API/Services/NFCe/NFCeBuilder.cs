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
            DestinatarioRequest? destinatario = null,
            string formaPagamento = "01",
            long? numeroReservado = null)
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
                configuracaoFiscal,
                numeroReservado
            );

            // Emitente
            nfce.infNFe.emit =
                _emitenteBuilder.Criar(empresa);

            // Destinatário / CPF na nota
            MontarDestinatario(
                nfce,
                destinatario,
                configuracaoFiscal
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
                    nfce.infNFe.total.ICMSTot.vNF,
                    formaPagamento
                );


            return nfce;
        }

        // ============================================================
        // Destinatário da NFC-e: CPF ou CNPJ, sem inscrição estadual.
        // Sem documento informado, a NFC-e permanece sem destinatário.
        // ============================================================
        private void MontarDestinatario( global::NFe.Classes.NFe nfce, DestinatarioRequest? destinatario, ConfiguracaoFiscal configuracaoFiscal)
        {
            if (destinatario == null ||
                string.IsNullOrWhiteSpace(destinatario.CpfCnpj))
                return;

            var documento = new string(destinatario.CpfCnpj
                .Where(char.IsDigit).ToArray());

            if (documento.Length == 11)
            {
                if (!CpfValido(documento))
                    throw new InvalidOperationException("O CPF informado é inválido.");
            }
            else if (documento.Length == 14)
            {
                if (!CnpjValido(documento))
                    throw new InvalidOperationException("O CNPJ informado é inválido.");
            }
            else
            {
                throw new InvalidOperationException("O destinatário deve possuir CPF (11 dígitos) ou CNPJ (14 dígitos).");
            }

            var destino = new dest(VersaoServico.Versao400)
            {
                indIEDest = indIEDest.NaoContribuinte
            };

            if (documento.Length == 11)
                destino.CPF = documento;
            else
                destino.CNPJ = documento;

            // Garante que o destinatário foi instanciado
            nfce.infNFe.dest ??= new dest(VersaoServico.Versao400);

            if (configuracaoFiscal.Ambiente == 2)
            {
                destino.xNome =
                    "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL";
            }
            else if (!string.IsNullOrWhiteSpace(destinatario.Nome))
            {
                destino.xNome = destinatario.Nome.Trim();
            }

            nfce.infNFe.dest = destino;
        }

        private static bool CnpjValido(string cnpj)
        {
            if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1)
                return false;

            var pesosPrimeiro = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            var pesosSegundo = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            static int Digito(string valor, int[] pesos)
            {
                var soma = 0;
                for (var i = 0; i < pesos.Length; i++)
                    soma += (valor[i] - '0') * pesos[i];
                var resto = soma % 11;
                return resto < 2 ? 0 : 11 - resto;
            }

            return cnpj[12] - '0' == Digito(cnpj, pesosPrimeiro) &&
                   cnpj[13] - '0' == Digito(cnpj, pesosSegundo);
        }

        // ============================================================
        // Identificação da NFC-e
        // ============================================================
        private void MontarIdentificacao(
            global::NFe.Classes.NFe nfce,
            Empresa empresa,
            ConfiguracaoFiscal configuracaoFiscal,
            long? numeroReservado)
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
                numeroReservado ?? configuracaoFiscal.ProximoNumeroNFCe;

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