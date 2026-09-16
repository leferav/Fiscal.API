using DFe.Classes.Flags;
using Fiscal.API.Models.NFe;
using NFe.Classes.Informacoes.Destinatario;

namespace Fiscal.API.Services.NFe
{
    public class DestinatarioBuilder
    {
        public dest Criar(DestinatarioRequest request)
        {
            if (!int.TryParse(request.CodigoMunicipio, out var codigoMunicipio))
            {
                throw new Exception(
                    "Código IBGE do município do destinatário inválido."
                );
            }

            var cep = new string(
                (request.Cep ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray()
            );

            if (cep.Length != 8)
            {
                throw new Exception(
                    "CEP do destinatário deve possuir 8 números."
                );
            }

            var documento = new string(
                (request.CpfCnpj ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray()
            );

            var destinatario = new dest(VersaoServico.Versao400)
            {
                xNome = request.Nome,



                enderDest = new enderDest
                {
                    xLgr = request.Logradouro,
                    nro = request.Numero,
                    xBairro = request.Bairro,

                    cMun = codigoMunicipio,
                    xMun = request.Municipio,
                    UF = request.Uf,

                    CEP = cep,

                    cPais = 1058,
                    xPais = "BRASIL"
                },

                email = request.Email
            };

            var ie = new string((request.InscricaoEstadual ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray()
);

            if (!string.IsNullOrWhiteSpace(ie))
            {
                destinatario.IE = ie;
                destinatario.indIEDest = indIEDest.ContribuinteICMS;
            }
            else
            {
                destinatario.indIEDest = indIEDest.NaoContribuinte;
            }

            if (documento.Length == 14)
            {
                destinatario.CNPJ = documento;
            }
            else if (documento.Length == 11)
            {
                destinatario.CPF = documento;
            }
            else
            {
                throw new Exception(
                    "CPF/CNPJ do destinatário inválido."
                );
            }

            return destinatario;
        }
    }
}