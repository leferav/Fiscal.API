using NFe.Classes.Informacoes.Transporte;

namespace Fiscal.API.Services.NFe
{
    public class TransporteBuilder
    {
        public transp Criar()
        {
            return new transp
            {
                modFrete = ModalidadeFrete.mfSemFrete
            };
        }
    }
}