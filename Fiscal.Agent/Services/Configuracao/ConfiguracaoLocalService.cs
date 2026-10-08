using Fiscal.Agent.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Fiscal.Agent.Services.Configuracao;

public class ConfiguracaoLocalService
{
    private readonly string _diretorioBase;
    private readonly string _caminhoConfiguracao;
    private readonly string _caminhoCertificado;

    public ConfiguracaoLocalService()
    {
        _diretorioBase = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "FiscalAgent");

        _caminhoConfiguracao = Path.Combine(
            _diretorioBase,
            "agent.json");

        _caminhoCertificado = Path.Combine(
            _diretorioBase,
            "certificado.pfx");
    }

    public void SalvarCertificado(
        string caminhoPfxOrigem,
        string senha)
    {
        if (!File.Exists(caminhoPfxOrigem))
        {
            throw new FileNotFoundException(
                "Certificado não encontrado.",
                caminhoPfxOrigem);
        }

        Directory.CreateDirectory(_diretorioBase);

        File.Copy(
            caminhoPfxOrigem,
            _caminhoCertificado,
            overwrite: true);

        var configuracao =
            Carregar() ?? new ConfiguracaoLocal();

        configuracao.CaminhoCertificado =
            _caminhoCertificado;

        configuracao.SenhaProtegida =
            ProtegerTexto(senha);

        SalvarConfiguracao(configuracao);
    }

    public void SalvarCredencialAgente(
        Guid agenteId,
        string credencial)
    {
        var configuracao =
            Carregar() ?? new ConfiguracaoLocal();

        configuracao.AgenteId =
            agenteId;

        configuracao.CredencialAgenteProtegida =
            ProtegerTexto(credencial);

        SalvarConfiguracao(configuracao);
    }

    public ConfiguracaoLocal? Carregar()
    {
        if (!File.Exists(_caminhoConfiguracao))
            return null;

        var json =
            File.ReadAllText(_caminhoConfiguracao);

        return JsonSerializer.Deserialize<ConfiguracaoLocal>(
            json);
    }

    public string DesprotegerSenha(
        string senhaProtegida)
    {
        return DesprotegerTexto(senhaProtegida);
    }

    public string DesprotegerCredencialAgente(
        string credencialProtegida)
    {
        return DesprotegerTexto(
            credencialProtegida);
    }

    private static string ProtegerTexto(
        string valor)
    {
        var bytes =
            Encoding.UTF8.GetBytes(valor);

        var bytesProtegidos =
            ProtectedData.Protect(
                bytes,
                null,
                DataProtectionScope.LocalMachine);

        return Convert.ToBase64String(
            bytesProtegidos);
    }

    private static string DesprotegerTexto(
        string valorProtegido)
    {
        var bytesProtegidos =
            Convert.FromBase64String(
                valorProtegido);

        var bytes =
            ProtectedData.Unprotect(
                bytesProtegidos,
                null,
                DataProtectionScope.LocalMachine);

        return Encoding.UTF8.GetString(bytes);
    }

    private void SalvarConfiguracao(
        ConfiguracaoLocal configuracao)
    {
        Directory.CreateDirectory(
            _diretorioBase);

        var json =
            JsonSerializer.Serialize(
                configuracao,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(
            _caminhoConfiguracao,
            json);
    }
}