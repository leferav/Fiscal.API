using Fiscal.API.Models.NFe;
using NFe.Classes.Informacoes.Detalhe;
using NFe.Classes.Informacoes.Detalhe.Tributacao;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.InformacoesIbsCbs;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.InformacoesIbsCbs.InformacoesIbs;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.Tipos;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual.Tipos;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Federal;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Federal.Tipos;

namespace Fiscal.API.Services.NFe
{
    public class ProdutoBuilder
    {
        private readonly IConfiguration _configuration;

        public ProdutoBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<det> Criar(List<ProdutoRequest> produtos)
        {
            if (produtos == null || produtos.Count == 0)
            {
                throw new Exception(
                    "Informe pelo menos um produto."
                );
            }

            var itens = new List<det>();

            var numeroItem = 1;

            var crt = int.Parse(
                _configuration["Fiscal:Emitente:Crt"] ?? "1"
            );

            var ambiente =
                _configuration["Fiscal:Ambiente"];

            foreach (var item in produtos)
            {
                var valorProduto =
                    item.Quantidade * item.ValorUnitario;

                var descricao = item.Descricao;

                // Obrigatório para o primeiro item em homologação
                if (
                    string.Equals(
                        ambiente,
                        "Homologacao",
                        StringComparison.OrdinalIgnoreCase
                    )
                    && numeroItem == 1
                )
                {
                    descricao =
                        "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL";
                }

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

                        CFOP = int.Parse(item.Cfop),

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

                                gCBS = new global::NFe.Classes.Informacoes.Detalhe.Tributacao.Compartilhado.InformacoesIbsCbs.InformacoesCbs.gCBS
                                {
                                    pCBS = 0.9m,
                                    vCBS = 0
                                }
                            }
                        },

                        ICMS = CriarIcms(
                            crt,
                            valorProduto
                        )
                    }
                });
            }

            return itens;
        }

        private ICMS CriarIcms(
            int crt,
            decimal valorProduto)
        {
            // Simples Nacional ou MEI
            if (crt == 1 || crt == 4)
            {
                return new ICMS
                {
                    TipoICMS = new ICMSSN102
                    {
                        CSOSN =
                            Csosnicms.Csosn102,

                        orig =
                            OrigemMercadoria
                                .OmNacional
                    }
                };
            }

            // Regime Normal
            if (crt == 3)
            {
                var cst =
                    _configuration[
                        "Fiscal:Tributacao:CstIcms"
                    ];

                var aliquotaTexto =
                    _configuration[
                        "Fiscal:Tributacao:AliquotaIcms"
                    ];

                if (string.IsNullOrWhiteSpace(cst))
                {
                    throw new Exception(
                        "CRT 3: informe Fiscal:Tributacao:CstIcms."
                    );
                }

                if (string.IsNullOrWhiteSpace(aliquotaTexto))
                {
                    throw new Exception(
                        "CRT 3: informe Fiscal:Tributacao:AliquotaIcms."
                    );
                }

                var aliquota = decimal.Parse(
                    aliquotaTexto,
                    System.Globalization
                        .CultureInfo.InvariantCulture
                );

                if (cst == "00")
                {
                    var valorIcms =
                        Math.Round(
                            valorProduto
                            * aliquota
                            / 100m,
                            2
                        );

                    return new ICMS
                    {
                        TipoICMS = new ICMS00
                        {
                            orig =
                                OrigemMercadoria
                                    .OmNacional,

                            CST =
                                Csticms.Cst00,

                            modBC = DeterminacaoBaseIcms.DbiValorOperacao,

                            vBC = valorProduto,

                            pICMS = aliquota,

                            vICMS = valorIcms
                        }
                    };
                }

                throw new Exception(
                    $"CST ICMS {cst} ainda não implementado."
                );
            }

            throw new Exception(
                $"CRT {crt} ainda não suportado."
            );
        }
    }
}