using Fiscal.Agent.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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



    public void SalvarCertificado(string caminhoPfxOrigem, string senha)
    {
        if (!File.Exists(caminhoPfxOrigem))
            throw new FileNotFoundException(
                "Certificado não encontrado.", caminhoPfxOrigem);

        if (string.IsNullOrEmpty(senha))
            throw new ArgumentException(
                "Informe a senha do certificado.");

        // 1. Validar o certificado antes de salvar.
        using (var certificado = new X509Certificate2(
            caminhoPfxOrigem,
            senha,
            X509KeyStorageFlags.EphemeralKeySet))
        {
            if (!certificado.HasPrivateKey)
                throw new InvalidOperationException(
                    "O certificado não possui chave privada.");

            if (DateTime.Now < certificado.NotBefore ||
                DateTime.Now > certificado.NotAfter)
                throw new InvalidOperationException(
                    "O certificado está fora do prazo de validade.");
        }

        Directory.CreateDirectory(_diretorioBase);

        // 2. Carregar a configuração atual.
        var configuracao = Carregar() ?? new ConfiguracaoLocal();

        // 3. Criar um arquivo exclusivo para o novo certificado.
        var nomeArquivo = $"certificado-{Guid.NewGuid():N}.pfx";

        var caminhoNovoCertificado = Path.Combine(
            _diretorioBase, nomeArquivo);

        var caminhoJsonTemporario = Path.Combine(
            _diretorioBase, $"{Guid.NewGuid():N}.json.tmp");

        var caminhoJsonBackup = _caminhoConfiguracao + ".bak";

        var configuracaoAtualizada = new ConfiguracaoLocal
        {
            CaminhoCertificado = caminhoNovoCertificado,
            SenhaProtegida = ProtegerTexto(senha),

            // Preservar a vinculação existente.
            AgenteId = configuracao.AgenteId,
            CredencialAgenteProtegida =
                configuracao.CredencialAgenteProtegida
        };

        bool configuracaoAtualizadaComSucesso = false;

        try
        {
            // 4. Copiar o novo PFX sem substituir o anterior.
            File.Copy(
                caminhoPfxOrigem,
                caminhoNovoCertificado,
                overwrite: false);

            // 5. Preparar o novo JSON.
            var json = JsonSerializer.Serialize(
                configuracaoAtualizada,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(caminhoJsonTemporario, json);

            // 6. Substituir a configuração de forma atômica
            // no mesmo volume, preservando um backup.
            if (File.Exists(_caminhoConfiguracao))
            {
                File.Replace(
                    caminhoJsonTemporario,
                    _caminhoConfiguracao,
                    caminhoJsonBackup);
            }
            else
            {
                File.Move(
                    caminhoJsonTemporario,
                    _caminhoConfiguracao);
            }

            configuracaoAtualizadaComSucesso = true;
        }
        finally
        {
            // Remover o JSON temporário, caso ainda exista.
            if (File.Exists(caminhoJsonTemporario))
                File.Delete(caminhoJsonTemporario);

            // Se a configuração não foi atualizada,
            // remover somente o novo PFX.
            if (!configuracaoAtualizadaComSucesso &&
                File.Exists(caminhoNovoCertificado))
            {
                File.Delete(caminhoNovoCertificado);
            }
        }
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