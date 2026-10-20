
using Fiscal.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Fiscal.API.Services.CertificadoDigital;

public class CertificadoDigitalService
{
    private readonly FiscalDbContext _context;
    private readonly CertificadoCriptografiaService _criptografia;

    public CertificadoDigitalService(
        FiscalDbContext context,
        CertificadoCriptografiaService criptografia)
    {
        _context = context;
        _criptografia = criptografia;
    }

    public async Task<(byte[] Pfx, string Senha)> ObterCertificadoAsync(
        Guid empresaId)
    {
        var certificado = await _context.CertificadosDigitais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Ativo)
            .Select(x => new
            {
                x.Id,
                x.ValidoDe,
                x.ValidoAte
            })
            .FirstOrDefaultAsync();

        if (certificado == null)
            throw new InvalidOperationException(
                "A empresa não possui certificado digital ativo.");

        var agora = DateTime.UtcNow;

        if (certificado.ValidoDe.HasValue &&
            agora < certificado.ValidoDe.Value)
            throw new InvalidOperationException(
                "O certificado digital ainda não está válido.");

        if (certificado.ValidoAte.HasValue &&
            agora > certificado.ValidoAte.Value)
            throw new InvalidOperationException(
                "O certificado digital está vencido.");

        var arquivo = await _context.CertificadosDigitaisArquivos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.CertificadoDigitalId == certificado.Id);

        if (arquivo == null)
            throw new InvalidOperationException(
                "Arquivo do certificado digital não encontrado.");

        byte[]? pfx = null;
        byte[]? senhaBytes = null;

        try
        {
            pfx = _criptografia.Descriptografar(
                arquivo.ConteudoCriptografado,
                arquivo.Nonce,
                arquivo.TagAutenticacao);

            senhaBytes = _criptografia.Descriptografar(
                arquivo.SenhaCriptografada,
                arquivo.SenhaNonce,
                arquivo.SenhaTagAutenticacao);

            var senha = Encoding.UTF8.GetString(senhaBytes);

            return (pfx, senha);
        }
        catch
        {
            if (pfx != null)
                CryptographicOperations.ZeroMemory(pfx);

            throw;
        }
        finally
        {
            if (senhaBytes != null)
                CryptographicOperations.ZeroMemory(senhaBytes);
        }
    }
}
