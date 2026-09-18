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

    public async Task<LoginResponse?> LoginAsync(
        string email,
        string senha)
    {
        var usuario = await _context.Usuarios
            .Include(x => x.Empresa)
            .FirstOrDefaultAsync(x =>
                x.Email == email &&
                x.Ativo);

        if (usuario == null)
            return null;

        var resultadoSenha =
            _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.SenhaHash,
                senha);

        if (resultadoSenha == PasswordVerificationResult.Failed)
            return null;

        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key não configurada.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var expirationMinutes =
            _configuration.GetValue<int>(
                "Jwt:ExpirationMinutes");

        var expiraEm = DateTime.UtcNow.AddMinutes(
            expirationMinutes);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                usuario.Id.ToString()
            ),

            new(
                ClaimTypes.Name,
                usuario.Nome
            ),

            new(
                ClaimTypes.Email,
                usuario.Email
            ),

            new(
                ClaimTypes.Role,
                usuario.Perfil
            ),

            new(
                "empresaId",
                usuario.EmpresaId.ToString()
            )
        };

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

        var token =
            new JwtSecurityTokenHandler()
                .WriteToken(jwt);

        return new LoginResponse
        {
            Token = token,
            ExpiraEm = expiraEm,

            Usuario = new UsuarioLoginResponse
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Perfil = usuario.Perfil
            },

            Empresa = new EmpresaLoginResponse
            {
                Id = usuario.Empresa.Id,
                RazaoSocial = usuario.Empresa.RazaoSocial,
                NomeFantasia = usuario.Empresa.NomeFantasia
            }
        };
    }
}