using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Utils;

namespace Fiscal.Agent.Services.Emissao;

/// <summary>
/// Configuração Zeus no computador do cliente. O PFX nunca é enviado à API.
/// Requer os mesmos pacotes Zeus e a pasta Schemas da API.
/// </summary>
public sealed class ZeusConfigurationFactory
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public ZeusConfigurationFactory(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public ConfiguracaoServico Criar()
    {
        var certificado = _configuration["Fiscal:Certificado"];
        var senha = _configuration["Fiscal:SenhaCertificado"];
        if (string.IsNullOrWhiteSpace(certificado))
            throw new InvalidOperationException("Fiscal:Certificado não configurado no Agent.");
        if (string.IsNullOrWhiteSpace(senha))
            throw new InvalidOperationException("Fiscal:SenhaCertificado não configurada no Agent.");

        var caminhoPfx = Path.IsPathRooted(certificado)
            ? certificado
            : Path.Combine(_environment.ContentRootPath, certificado);
        if (!File.Exists(caminhoPfx))
            throw new FileNotFoundException("Certificado A1 não encontrado no Agent.", caminhoPfx);

        var schemas = _configuration["Fiscal:DiretorioSchemas"] ?? "Schemas";
        var caminhoSchemas = Path.IsPathRooted(schemas)
            ? schemas
            : Path.Combine(_environment.ContentRootPath, schemas);
        if (!Directory.Exists(caminhoSchemas))
            throw new DirectoryNotFoundException($"Schemas Zeus não encontrados: {caminhoSchemas}");

        // Mantém os mesmos parâmetros comprovados em homologação na API.
        // Antes de habilitar produção/multi-UF, parametrizar cUF e tpAmb por solicitação.
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
