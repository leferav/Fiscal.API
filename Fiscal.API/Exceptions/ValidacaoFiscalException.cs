using System;

namespace Fiscal.API.Exceptions
{
    public class ValidacaoFiscalException : Exception
    {
        public ValidacaoFiscalException(string mensagem)
            : base(mensagem)
        {
        }

        public ValidacaoFiscalException(
            string mensagem,
            Exception innerException)
            : base(mensagem, innerException)
        {
        }
    }
}
