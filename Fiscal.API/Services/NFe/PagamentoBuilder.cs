
using NFe.Classes.Informacoes.Pagamento;

namespace Fiscal.API.Services.NFe
{
    public class PagamentoBuilder
    {
        public List<pag> Criar(
            decimal valorTotal,
            string formaPagamento = "01")
        {
            FormaPagamento tipoPagamento = formaPagamento switch
            {
                "01" => FormaPagamento.fpDinheiro,
                "03" => FormaPagamento.fpCartaoCredito,
                "04" => FormaPagamento.fpCartaoDebito,
                "17" => (FormaPagamento)17,
                _ => throw new ArgumentException(
                    $"Forma de pagamento inválida: {formaPagamento}")
            };

            return new List<pag>
            {
                new pag
                {
                    detPag = new List<detPag>
                    {
                        new detPag
                        {
                            tPag = tipoPagamento,
                            vPag = valorTotal
                        }
                    }
                }
            };
        }
    }
}
