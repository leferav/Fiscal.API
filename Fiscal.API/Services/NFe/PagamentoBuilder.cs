using NFe.Classes.Informacoes.Pagamento;

namespace Fiscal.API.Services.NFe
{
    public class PagamentoBuilder
    {
        public List<pag> Criar(decimal valorTotal)
        {
            return new List<pag>
            {
                new pag
                {
                    detPag = new List<detPag>
                    {
                        new detPag
                        {
                            tPag = FormaPagamento.fpDinheiro,
                            vPag = valorTotal
                        }
                    }
                }
            };
        }
    }
}