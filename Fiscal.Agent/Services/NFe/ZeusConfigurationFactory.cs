using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Utils;

namespace Fiscal.Agent.Services.NFe;

public class ZeusConfigurationFactory
{
    public ConfiguracaoServico Criar(
        string caminhoPfx,
        string senha)
    {
        if (!File.Exists(caminhoPfx))
            throw new FileNotFoundException(
                "Certificado não encontrado.", caminhoPfx);

        var caminhoSchemas = Path.Combine(
            AppContext.BaseDirectory, "Schemas");

        if (!Directory.Exists(caminhoSchemas))
            throw new DirectoryNotFoundException(
                $"Schemas não encontrados: {caminhoSchemas}");

        return new ConfiguracaoServico
        {
            cUF = Estado.GO,
            tpAmb = TipoAmbiente.Homologacao,
            VersaoLayout = VersaoServico.Versao400,
            ModeloDocumento = ModeloDocumento.NFCe,
            tpEmis = TipoEmissao.teNormal,

            TimeOut = 30000,
            DefineVersaoServicosAutomaticamente = true,

            ValidarSchemas = true,
            DiretorioSchemas = caminhoSchemas,

            Certificado = new ConfiguracaoCertificado
            {
                TipoCertificado = TipoCertificado.A1Arquivo,
                Arquivo = caminhoPfx,
                Senha = senha
            }
        };
    }
}