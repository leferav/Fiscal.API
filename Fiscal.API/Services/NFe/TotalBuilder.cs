using NFe.Classes.Informacoes.Detalhe;
using NFe.Classes.Informacoes.Detalhe.Tributacao.Estadual;
using NFe.Classes.Informacoes.Total;
using NFe.Classes.Informacoes.Total.IbsCbs;
using NFe.Classes.Informacoes.Total.IbsCbs.Cbs;
using NFe.Classes.Informacoes.Total.IbsCbs.Ibs;

namespace Fiscal.API.Services.NFe
{
    public class TotalBuilder
    {
        public total Criar(List<det> itens)
        {
            var valorProdutos =
                itens.Sum(x => x.prod.vProd);

            var valorDesconto =
                itens.Sum(x => x.prod.vDesc ?? 0);

            var valorTributos =
                itens.Sum(x => x.imposto.vTotTrib ?? 0);

            decimal valorBaseIcms = 0;
            decimal valorIcms = 0;

            foreach (var item in itens)
            {
                var tipoIcms = item.imposto?.ICMS?.TipoICMS;

                // Regime Normal - CST 00
                if (tipoIcms is ICMS00 icms00)
                {
                    valorBaseIcms += icms00.vBC;
                    valorIcms += icms00.vICMS;
                }

                // ICMSSN102 não possui destaque de ICMS,
                // portanto não soma vBC/vICMS.
            }

            var icmsTot = new ICMSTot
            {
                vBC = valorBaseIcms,
                vICMS = valorIcms,

                vICMSDeson = 0,

                vFCP = 0,

                vBCST = 0,
                vST = 0,

                vFCPST = 0,
                vFCPSTRet = 0,

                vProd = valorProdutos,

                vFrete = 0,
                vSeg = 0,
                vDesc = valorDesconto,

                vII = 0,
                vIPI = 0,
                vIPIDevol = 0,

                vPIS = 0,
                vCOFINS = 0,

                vOutro = 0,

                vTotTrib = valorTributos
            };

            icmsTot.vNF =
                icmsTot.vProd
                - icmsTot.vDesc
                - icmsTot.vICMSDeson.GetValueOrDefault()
                + icmsTot.vST
                + icmsTot.vFCPST.GetValueOrDefault()
                + icmsTot.vFrete
                + icmsTot.vSeg
                + icmsTot.vOutro
                + icmsTot.vII
                + icmsTot.vIPI
                + icmsTot.vIPIDevol.GetValueOrDefault();

            var ibsCbsTot = new IBSCBSTot
            {
                vBCIBSCBS = 0,

                gIBS = new gIBS
                {
                    gIBSUF = new gIBSUFTotal
                    {
                        vDif = 0,
                        vDevTrib = 0,
                        vIBSUF = 0
                    },

                    gIBSMun = new gIBSMunTotal
                    {
                        vDif = 0,
                        vDevTrib = 0,
                        vIBSMun = 0
                    },

                    vIBS = 0,
                    vCredPres = 0,
                    vCredPresCondSus = 0
                },

                gCBS = new gCBSTotal
                {
                    vDif = 0,
                    vDevTrib = 0,
                    vCBS = 0,
                    vCredPres = 0,
                    vCredPresCondSus = 0
                }
            };

            return new total
            {
                ICMSTot = icmsTot,
                IBSCBSTot = ibsCbsTot
            };
        }
    }
}