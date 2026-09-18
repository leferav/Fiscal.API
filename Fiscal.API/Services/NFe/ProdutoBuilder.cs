using Fiscal.API.Models.Database;
using Fiscal.API.Models.NFe;
using NFe.Classes.Informacoes.Detalhe;
using NFe.Classes.Informacoes.Detalhe.Tributacao;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.InformacoesIbsCbs;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.InformacoesIbsCbs.InformacoesIbs;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.Tipos;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual.Tipos;

namespace Fiscal.API.Services.NFe
{
    public class ProdutoBuilder
    {
        // Mantido apenas para o NFeBuilder antigo continuar compilando.
        // A NF-e será refatorada posteriormente para o novo modelo de produtos.
        public List<det> Criar(List<ProdutoRequest> produtos)
        {
            throw new NotSupportedException(
                "A emissão de NF-e ainda não foi migrada para o novo cadastro de produtos.");
        }

        public List<det> CriarNFCe(
            List<ItemFiscal> itensFiscais,
            Empresa empresa,
            ConfiguracaoFiscal configuracaoFiscal)
        {
            if (itensFiscais == null || itensFiscais.Count == 0)
            {
                throw new Exception(
                    "Informe pelo menos um produto."
                );
            }

            var itens = new List<det>();

            var numeroItem = 1;

            var ambienteHomologacao =
                configuracaoFiscal.Ambiente == 2;

            foreach (var item in itensFiscais)
            {
                var configuracaoTributaria =
                    item.ConfiguracaoTributaria;

                // ====================================================
                // Valores
                // ====================================================

                var valorProduto =
                    item.Quantidade * item.ValorUnitario;

                var descricao = item.Descricao;

                // ====================================================
                // Homologação
                // ====================================================

                if (ambienteHomologacao && numeroItem == 1)
                {
                    descricao =
                        "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL";
                }

                // ====================================================
                // Valida CFOP
                // ====================================================

                if (string.IsNullOrWhiteSpace(
                    configuracaoTributaria.Cfop))
                {
                    throw new Exception(
                        $"CFOP não informado para o produto {item.Codigo}."
                    );
                }

                // ====================================================
                // Monta item
                // ====================================================

                itens.Add(new det
                {
                    nItem = numeroItem++,

                    prod = new prod
                    {
                        cProd = item.Codigo,

                        cEAN = "SEM GTIN",
                        cEANTrib = "SEM GTIN",

                        xProd = descricao,

                        NCM = item.Ncm,

                        CFOP = int.Parse(
                            configuracaoTributaria.Cfop
                        ),

                        uCom = item.Unidade,
                        qCom = item.Quantidade,
                        vUnCom = item.ValorUnitario,

                        uTrib = item.Unidade,
                        qTrib = item.Quantidade,
                        vUnTrib = item.ValorUnitario,

                        vProd = valorProduto,

                        indTot =
                            IndicadorTotal
                                .ValorDoItemCompoeTotalNF
                    },

                    imposto = new imposto
                    {
                        // =============================================
                        // IBS / CBS
                        // =============================================

                        IBSCBS = new IBSCBS
                        {
                            CST = CST.Cst000,

                            cClassTrib = "000001",

                            gIBSCBS = new gIBSCBS
                            {
                                vBC = 0,

                                gIBSUF = new gIBSUF
                                {
                                    pIBSUF = 0.1m,
                                    vIBSUF = 0
                                },

                                gIBSMun = new gIBSMun
                                {
                                    pIBSMun = 0,
                                    vIBSMun = 0
                                },

                                vIBS = 0,

                                gCBS =
                                    new global::NFe.Classes.Informacoes.Detalhe
                                        .Tributacao.Compartilhado
                                        .InformacoesIbsCbs
                                        .InformacoesCbs.gCBS
                                    {
                                        pCBS = 0.9m,
                                        vCBS = 0
                                    }
                            }
                        },

                        // =============================================
                        // ICMS
                        // =============================================

                        ICMS = CriarIcms(
                            empresa.Crt,
                            valorProduto,
                            configuracaoTributaria
                        )
                    }
                });
            }

            return itens;
        }

        // ============================================================
        // ICMS
        // ============================================================
        private ICMS CriarIcms(
            int crt,
            decimal valorProduto,
            ConfiguracaoTributaria configuracaoTributaria)
        {
            // ========================================================
            // Simples Nacional / MEI
            // ========================================================

            if (crt == 1 || crt == 4)
            {
                return CriarIcmsSimplesNacional(
                    configuracaoTributaria
                );
            }

            // ========================================================
            // Regime Normal
            // ========================================================

            if (crt == 3)
            {
                return CriarIcmsRegimeNormal(
                    valorProduto,
                    configuracaoTributaria
                );
            }

            throw new Exception(
                $"CRT {crt} ainda não suportado."
            );
        }

        // ============================================================
        // Simples Nacional / MEI
        // ============================================================
        private ICMS CriarIcmsSimplesNacional(
            ConfiguracaoTributaria configuracaoTributaria)
        {
            var csosn = configuracaoTributaria.Csosn ?? "102";

            if (csosn == "102")
            {
                return new ICMS
                {
                    TipoICMS = new ICMSSN102
                    {
                        CSOSN = Csosnicms.Csosn102,
                        orig = OrigemMercadoria.OmNacional
                    }
                };
            }

            throw new Exception(
                $"CSOSN {csosn} ainda não implementado.");
        }

        private ICMS CriarIcmsRegimeNormal(
            decimal valorProduto,
            ConfiguracaoTributaria configuracaoTributaria)
        {
            var cst = configuracaoTributaria.CstIcms;
            var aliquota = configuracaoTributaria.AliquotaIcms;

            if (string.IsNullOrWhiteSpace(cst))
            {
                throw new Exception(
                    "CRT 3: CST ICMS não informado.");
            }

            if (cst == "00")
            {
                var valorIcms = Math.Round(
                    valorProduto * aliquota / 100m,
                    2);

                return new ICMS
                {
                    TipoICMS = new ICMS00
                    {
                        orig = OrigemMercadoria.OmNacional,
                        CST = Csticms.Cst00,
                        modBC = DeterminacaoBaseIcms.DbiValorOperacao,
                        vBC = valorProduto,
                        pICMS = aliquota,
                        vICMS = valorIcms
                    }
                };
            }

            throw new Exception(
                $"CST ICMS {cst} ainda não implementado.");
        }

    }
}
