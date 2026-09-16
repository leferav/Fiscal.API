using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using NFe.Classes.Informacoes.Emitente;

namespace Fiscal.API.Services.NFe
{
    public class EmitenteBuilder
    {
        private readonly IConfiguration _configuration;

        public EmitenteBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public emit Criar()
        {
            var codigoMunicipio = int.Parse(
                _configuration["Fiscal:Emitente:CodigoMunicipio"]!
            );

            var cep = new string(
                (_configuration["Fiscal:Emitente:Cep"] ?? "")
                .Where(char.IsDigit)
                .ToArray()
            );

            return new emit
            {
                CNPJ = _configuration["Fiscal:Emitente:Cnpj"],
                IE = _configuration["Fiscal:Emitente:InscricaoEstadual"],
                xNome = _configuration["Fiscal:Emitente:RazaoSocial"],
                xFant = _configuration["Fiscal:Emitente:NomeFantasia"],

                CRT = (CRT)int.Parse(
                    _configuration["Fiscal:Emitente:Crt"] ?? "1"
                ),

                enderEmit = new enderEmit
                {
                    xLgr = _configuration["Fiscal:Emitente:Logradouro"],
                    nro = _configuration["Fiscal:Emitente:Numero"],
                    xBairro = _configuration["Fiscal:Emitente:Bairro"],
                    cMun = codigoMunicipio,
                    xMun = _configuration["Fiscal:Emitente:Municipio"],
                    UF = Estado.GO,
                    CEP = cep,
                    cPais = 1058,
                    xPais = "BRASIL"
                }
            };
        }
    }
}