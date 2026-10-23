using Fiscal.API.Data;
using Fiscal.API.DTOs.Auth;
using Fiscal.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Fiscal.API.Services;

public class AuthService
{
    private readonly FiscalDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<Usuario> _passwordHasher;

    public AuthService(
        FiscalDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<Usuario>();
    }


    public async Task<ResultadoLogin> LoginAsync( string email, string senha)
    {
        var usuario = await _context.Usuarios
            .Include(x => x.UsuariosEmpresas)
                .ThenInclude(x => x.Empresa)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (usuario == null)
        {
            return new ResultadoLogin
            {
                Status = StatusLogin.CredenciaisInvalidas
            };
        }

        // Primeiro validar a senha.
        var resultadoSenha = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.SenhaHash,
            senha);

        if (resultadoSenha == PasswordVerificationResult.Failed)
        {
            return new ResultadoLogin
            {
                Status = StatusLogin.CredenciaisInvalidas
            };
        }

        // Verificar se a conta global está ativa.
        if (!usuario.Ativo)
        {
            return new ResultadoLogin
            {
                Status = StatusLogin.UsuarioInativo
            };
        }

        UsuarioEmpresa? usuarioEmpresa = null;
        string perfil;

        // ============================================================
        // IDENTIFICAÇÃO DO PERFIL
        // ============================================================

        if (usuario.AdministradorGeral)
        {
            // Master possui acesso global.
            perfil = "Master";
        }
        else
        {
            var vinculosAtivos = usuario.UsuariosEmpresas
                .Where(x => x.Ativo)
                .ToList();

            if (vinculosAtivos.Count == 0)
            {
                return new ResultadoLogin
                {
                    Status = StatusLogin.UsuarioInativo
                };
            }

            if (vinculosAtivos.Count > 1)
            {
                return new ResultadoLogin
                {
                    Status = StatusLogin.MultiplasEmpresas
                };
            }

            usuarioEmpresa = vinculosAtivos[0];
            perfil = usuarioEmpresa.Perfil;
        }

        // ============================================================
        // CONFIGURAÇÕES JWT
        // ============================================================

        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key não configurada.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Jwt:Issuer não configurado.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Jwt:Audience não configurado.");

        var expirationMinutes =
            _configuration.GetValue<int>(
                "Jwt:ExpirationMinutes");

        if (expirationMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Jwt:ExpirationMinutes inválido.");
        }

        var expiraEm = DateTime.UtcNow.AddMinutes(
            expirationMinutes);

        // ============================================================
        // CLAIMS DO USUÁRIO
        // ============================================================

        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Name, usuario.Nome),
        new(ClaimTypes.Email, usuario.Email),
        new(ClaimTypes.Role, perfil),
        new(
            "administradorGeral",
            usuario.AdministradorGeral
                .ToString()
                .ToLowerInvariant()
        )
    };

        // Somente usuários vinculados recebem empresaId.
        if (usuarioEmpresa != null)
        {
            claims.Add(new Claim(
                "empresaId",
                usuarioEmpresa.EmpresaId.ToString()
            ));
        }

        // ============================================================
        // GERAÇÃO DO TOKEN JWT
        // ============================================================

        var chave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var credenciais = new SigningCredentials(
            chave,
            SecurityAlgorithms.HmacSha256
        );

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: credenciais
        );

        var token = new JwtSecurityTokenHandler()
            .WriteToken(jwt);

        // ============================================================
        // RETORNO DO LOGIN
        // ============================================================

        return new ResultadoLogin
        {
            Status = StatusLogin.Sucesso,

            Dados = new LoginResponse
            {
                Token = token,
                ExpiraEm = expiraEm,

                Usuario = new UsuarioLoginResponse
                {
                    Id = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    Perfil = perfil
                },

                Empresa = usuarioEmpresa == null
                    ? null
                    : new EmpresaLoginResponse
                    {
                        Id = usuarioEmpresa.Empresa.Id,
                        RazaoSocial = usuarioEmpresa.Empresa.RazaoSocial,
                        NomeFantasia = usuarioEmpresa.Empresa.NomeFantasia
                    }
            }
        };
    }

    public async Task<bool> PodeSelecionarEmpresaAsync( Guid usuarioId, Guid empresaId)
    {
        var usuarioMaster = await _context.Usuarios
            .AnyAsync(x =>
                x.Id == usuarioId &&
                x.Ativo &&
                x.AdministradorGeral);

        if (!usuarioMaster)
            return false;

        return await _context.Empresas
            .AnyAsync(x =>
                x.Id == empresaId &&
                x.Ativo);
    }

    public async Task<(string Token, DateTime ExpiraEm)?> GerarTokenMasterEmpresaAsync( Guid usuarioId, Guid empresaId, DateTime limiteExpiracaoUtc)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == usuarioId &&
                x.Ativo &&
                x.AdministradorGeral);

        if (usuario == null)
            return null;

        var empresaValida = await _context.Empresas
            .AnyAsync(x => x.Id == empresaId && x.Ativo);

        if (!empresaValida)
            return null;

        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key não configurada.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer não configurado.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience não configurado.");

        // A troca de empresa não renova a sessão original.
        var expiraEm = limiteExpiracaoUtc.ToUniversalTime();

        if (expiraEm <= DateTime.UtcNow)
            return null;

        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Name, usuario.Nome),
        new(ClaimTypes.Email, usuario.Email),
        new(ClaimTypes.Role, "Master"),
        new("administradorGeral", "true"),
        new("empresaId", empresaId.ToString())
    };

        var chave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credenciais = new SigningCredentials(
            chave,
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: credenciais);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return (token, expiraEm);
    }

}