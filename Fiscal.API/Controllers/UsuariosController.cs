
using Fiscal.API.Constants;
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
                return Conflict( MensagensErro.AdministradorExistente);
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



    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar( Guid id, [FromBody] AtualizarUsuarioRequest request)
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
            if (request.EmpresaId.HasValue)
                return BadRequest(
                    "Não é permitido informar EmpresaId.");

            var empresaClaim =
                User.FindFirstValue("empresaId");

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

        var vinculo = await _context.UsuariosEmpresas
            .Include(x => x.Usuario)
            .FirstOrDefaultAsync(x =>
                x.UsuarioId == id &&
                x.EmpresaId == empresaId);

        if (vinculo == null)
            return NotFound("Usuário não encontrado nesta empresa.");

        // Não permitir alteração de contas Master.
        if (vinculo.Usuario.AdministradorGeral)
            return Forbid();

        // Administrador não pode editar outro Administrador
        // nem alterar sua própria conta por este endpoint.
        if (!master &&
            (vinculo.Perfil == "Administrador" ||
             id == usuarioId))
        {
            return Forbid();
        }

        var perfil = request.Perfil?.Trim();

        if (perfil != "Administrador" &&
            perfil != "Operador" &&
            perfil != "Consulta")
        {
            return BadRequest("Perfil inválido.");
        }

        // Somente Master pode atribuir Administrador.
        if (!master && perfil == "Administrador")
            return Forbid();

        // Impedir que uma empresa tenha dois Administradores.
        if (perfil == "Administrador" &&
            vinculo.Perfil != "Administrador")
        {
            var existeAdministrador =
                await _context.UsuariosEmpresas
                    .AnyAsync(x =>
                        x.EmpresaId == empresaId &&
                        x.UsuarioId != id &&
                        x.Perfil == "Administrador" &&
                        x.Ativo);

            if (existeAdministrador)
            {
                return Conflict(
                    "Esta empresa já possui um Administrador.");
            }
        }

        // Evitar deixar a empresa sem Administrador
        // ao trocar o perfil do Administrador atual.
        if (vinculo.Perfil == "Administrador" &&
            perfil != "Administrador")
        {
            return Conflict(
                "Substitua o Administrador antes de alterar seu perfil.");
        }

        var nome = request.Nome?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Nome e e-mail são obrigatórios.");
        }

        var emailExistente = await _context.Usuarios
            .AnyAsync(x =>
                x.Email == email &&
                x.Id != id);

        if (emailExistente)
            return Conflict("E-mail já cadastrado.");

        vinculo.Usuario.Nome = nome;
        vinculo.Usuario.Email = email;
        vinculo.Perfil = perfil;

        // Compatibilidade temporária com os campos antigos.
        if (vinculo.Usuario.EmpresaId == empresaId)
            vinculo.Usuario.Perfil = perfil;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            vinculo.Usuario.Id,
            vinculo.Usuario.Nome,
            vinculo.Usuario.Email,
            vinculo.EmpresaId,
            vinculo.Perfil,
            Ativo = vinculo.Ativo && vinculo.Usuario.Ativo
        });
    }



    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> AlterarStatus( Guid id, [FromBody] AlterarStatusUsuarioRequest request)
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

        if (!request.Ativo.HasValue)
            return BadRequest("Informe o status do usuário.");

        bool master = usuarioLogado.AdministradorGeral;
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
            if (request.EmpresaId.HasValue)
                return BadRequest(
                    "Não é permitido informar EmpresaId.");

            var empresaClaim =
                User.FindFirstValue("empresaId");

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

        var vinculo = await _context.UsuariosEmpresas
            .Include(x => x.Usuario)
            .FirstOrDefaultAsync(x =>
                x.UsuarioId == id &&
                x.EmpresaId == empresaId);

        if (vinculo == null)
            return NotFound("Usuário não encontrado nesta empresa.");

        // Não permitir alteração de contas Master.
        if (vinculo.Usuario.AdministradorGeral)
            return Forbid();

        // Administrador comum não pode alterar outro
        // Administrador nem sua própria conta.
        if (!master &&
            (vinculo.Perfil == "Administrador" ||
             id == usuarioId))
        {
            return Forbid();
        }

        // Preservar o Administrador da empresa.
        // A substituição será um fluxo separado.
        if (vinculo.Perfil == "Administrador" &&
            !request.Ativo.Value)
        {
            return Conflict(
                "Não é permitido desativar o Administrador. " +
                "Realize a substituição primeiro.");
        }

        if (vinculo.Ativo == request.Ativo.Value)
        {
            return Ok(new
            {
                vinculo.Usuario.Id,
                vinculo.Usuario.Nome,
                vinculo.EmpresaId,
                vinculo.Perfil,
                vinculo.Ativo,
                Mensagem = "O usuário já possui este status."
            });
        }

        // Altera somente o vínculo com a empresa.
        vinculo.Ativo = request.Ativo.Value;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            vinculo.Usuario.Id,
            vinculo.Usuario.Nome,
            vinculo.EmpresaId,
            vinculo.Perfil,
            vinculo.Ativo,
            Mensagem = vinculo.Ativo
                ? "Usuário ativado com sucesso."
                : "Usuário desativado com sucesso."
        });
    }



}
