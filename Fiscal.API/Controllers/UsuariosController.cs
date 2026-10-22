
using Fiscal.API.Data;
using Fiscal.API.DTOs.Usuarios;
using Fiscal.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Fiscal.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly FiscalDbContext _context;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public UsuariosController(FiscalDbContext context)
    {
        _context = context;
    }


    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CriarUsuarioRequest request)
    {
        var usuarioClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioClaim, out var usuarioId))
            return Unauthorized("Usuário não identificado.");

        var usuarioLogado = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == usuarioId && x.Ativo);

        if (usuarioLogado == null)
            return Forbid();

        var master = usuarioLogado.AdministradorGeral;
        Guid empresaId;

        if (master)
        {
            if (!request.EmpresaId.HasValue ||
                request.EmpresaId.Value == Guid.Empty)
            {
                return BadRequest(
                    "O Master deve informar a empresa.");
            }

            empresaId = request.EmpresaId.Value;
        }
        else
        {
            // Administrador não pode escolher a empresa.
            if (request.EmpresaId.HasValue)
            {
                return BadRequest(
                    "Não é permitido informar EmpresaId.");
            }

            var empresaClaim = User.FindFirstValue("empresaId");

            if (!Guid.TryParse(empresaClaim, out empresaId))
                return Unauthorized("Empresa não identificada.");

            var administrador = await _context.UsuariosEmpresas
                .AnyAsync(x =>
                    x.UsuarioId == usuarioId &&
                    x.EmpresaId == empresaId &&
                    x.Ativo &&
                    x.Usuario.Ativo &&
                    x.Perfil == "Administrador");

            if (!administrador)
                return Forbid();
        }

        var perfil = request.Perfil?.Trim();

        if (master)
        {
            if (perfil != "Administrador" &&
                perfil != "Operador" &&
                perfil != "Consulta")
            {
                return BadRequest("Perfil inválido.");
            }
        }
        else
        {
            if (perfil != "Operador" &&
                perfil != "Consulta")
            {
                return BadRequest(
                    "Perfil permitido: Operador ou Consulta.");
            }
        }

        var empresaExiste = await _context.Empresas
            .AnyAsync(x => x.Id == empresaId);

        if (!empresaExiste)
            return BadRequest("Empresa não encontrada.");

        if (perfil == "Administrador")
        {
            var jaExisteAdministrador =
                await _context.UsuariosEmpresas
                    .AnyAsync(x =>
                        x.EmpresaId == empresaId &&
                        x.Perfil == "Administrador" &&
                        x.Ativo);

            if (jaExisteAdministrador)
            {
                return Conflict(
                    "Esta empresa já possui um Administrador.");
            }
        }

        var nome = request.Nome?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Senha))
        {
            return BadRequest("Dados obrigatórios inválidos.");
        }

        var emailExistente = await _context.Usuarios
            .AnyAsync(x => x.Email == email);

        if (emailExistente)
        {
            return Conflict(
                "E-mail já cadastrado. Utilize o fluxo de convite.");
        }

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            EmpresaId = empresaId,
            Perfil = perfil!,
            Ativo = true,
            AdministradorGeral = false,
            CriadoEm = DateTime.UtcNow
        };

        usuario.SenhaHash = _hasher.HashPassword(
            usuario, request.Senha);

        var vinculo = new UsuarioEmpresa
        {
            UsuarioId = usuario.Id,
            EmpresaId = empresaId,
            Perfil = perfil!,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        _context.UsuariosEmpresas.Add(vinculo);

        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            vinculo.Perfil,
            vinculo.EmpresaId,
            usuario.Ativo
        });
    }


    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] Guid? empresaId)
    {
        var usuarioClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioClaim, out var usuarioId))
            return Unauthorized("Usuário não identificado.");

        var usuarioLogado = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == usuarioId && x.Ativo);

        if (usuarioLogado == null)
            return Forbid();

        var master = usuarioLogado.AdministradorGeral;

        IQueryable<UsuarioEmpresa> consulta =
            _context.UsuariosEmpresas.AsNoTracking();

        if (master)
        {
            // Master pode consultar todas as empresas
            // ou filtrar por uma empresa específica.
            if (empresaId.HasValue)
            {
                consulta = consulta.Where(x =>
                    x.EmpresaId == empresaId.Value);
            }
        }
        else
        {
            // Administrador não pode escolher empresa.
            if (empresaId.HasValue)
            {
                return BadRequest(
                    "Não é permitido informar EmpresaId.");
            }

            var empresaClaim =
                User.FindFirstValue("empresaId");

            if (!Guid.TryParse(empresaClaim, out var empresaLogadaId))
                return Unauthorized("Empresa não identificada.");

            var administrador = await _context.UsuariosEmpresas
                .AnyAsync(x =>
                    x.UsuarioId == usuarioId &&
                    x.EmpresaId == empresaLogadaId &&
                    x.Ativo &&
                    x.Usuario.Ativo &&
                    x.Perfil == "Administrador");

            if (!administrador)
                return Forbid();

            consulta = consulta.Where(x =>
                x.EmpresaId == empresaLogadaId);
        }

        var usuarios = await consulta
            .OrderBy(x => x.Usuario.Nome)
            .Select(x => new
            {
                x.Usuario.Id,
                x.Usuario.Nome,
                x.Usuario.Email,
                x.EmpresaId,
                Empresa = x.Empresa.NomeFantasia,
                x.Perfil,
                Ativo = x.Ativo && x.Usuario.Ativo,
                x.CriadoEm
            })
            .ToListAsync();

        return Ok(usuarios);
    }


}
