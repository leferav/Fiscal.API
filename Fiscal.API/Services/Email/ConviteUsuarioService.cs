
using System.Security.Cryptography;
using System.Text;
using Fiscal.API.Data;
using Fiscal.API.DTOs.Auth;
using Fiscal.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Services.Email;

public class ConviteUsuarioService
{
    private readonly FiscalDbContext _context;

    public ConviteUsuarioService(FiscalDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Sucesso, string Mensagem)>
        AceitarAsync(AceitarConviteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.Senha) ||
            request.Senha.Length < 12)
        {
            return (false, "Convite ou senha inválidos.");
        }

        if (request.Senha != request.ConfirmarSenha)
        {
            return (false, "As senhas não coincidem.");
        }

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(request.Token)));

        await using var transacao =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var convite = await _context.ConvitesUsuarios
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash);

        if (convite == null ||
            convite.UtilizadoEm != null ||
            convite.ExpiraEm <= DateTime.UtcNow)
        {
            return (false, "Convite inválido ou expirado.");
        }

        var empresaAtiva = await _context.Empresas
            .AnyAsync(x =>
                x.Id == convite.EmpresaId && x.Ativo);

        if (!empresaAtiva)
        {
            return (false, "Empresa indisponível.");
        }

        var email = convite.Email.Trim().ToLowerInvariant();

        var emailExistente = await _context.Usuarios
            .AnyAsync(x => x.Email.ToLower() == email);

        if (emailExistente)
        {
            return (false, "E-mail já cadastrado.");
        }

        var usuario = new Usuario
        {
            EmpresaId = convite.EmpresaId,
            Nome = convite.Nome,
            Email = email,
            Perfil = convite.Perfil,
            AdministradorGeral = false,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        usuario.SenhaHash =
            new PasswordHasher<Usuario>()
                .HashPassword(usuario, request.Senha);

        _context.Usuarios.Add(usuario);

        _context.UsuariosEmpresas.Add(
            new UsuarioEmpresa
            {
                UsuarioId = usuario.Id,
                EmpresaId = convite.EmpresaId,
                Perfil = convite.Perfil,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            });

        convite.UtilizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transacao.CommitAsync();

        return (true, "Conta ativada com sucesso.");
    }
}
