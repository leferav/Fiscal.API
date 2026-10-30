using Fiscal.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Fiscal.API.Controllers;

[ApiController]
[Authorize(Roles = "Master,Administrador")]
[Route("api/logs-fiscais")]
public class LogsFiscaisController : ControllerBase
{
    private readonly FiscalDbContext _db;

    public LogsFiscaisController(FiscalDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? empresaId,
        [FromQuery] Guid? notaFiscalId,
        [FromQuery] long? numero,
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] string? evento,
        [FromQuery] int? cStat,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 30,
        CancellationToken cancellationToken = default)
    {
        if (pagina < 1 || tamanhoPagina < 1 || tamanhoPagina > 100)
            return BadRequest(new { mensagem = "Página deve ser >= 1 e tamanhoPagina entre 1 e 100." });
        if (inicio.HasValue && fim.HasValue && inicio.Value > fim.Value)
            return BadRequest(new { mensagem = "Data inicial posterior à final." });
        if (inicio.HasValue && inicio.Value.Kind != DateTimeKind.Utc ||
            fim.HasValue && fim.Value.Kind != DateTimeKind.Utc)
            return BadRequest(new { mensagem = "Envie datas em UTC, por exemplo 2026-10-30T00:00:00Z." });

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var usuario = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new { u.Ativo, u.AdministradorGeral })
            .FirstOrDefaultAsync(cancellationToken);
        if (usuario == null || !usuario.Ativo)
            return Forbid();

        Guid? empresaPermitida = null;
        if (!usuario.AdministradorGeral)
        {
            if (!User.IsInRole("Administrador") ||
                !Guid.TryParse(User.FindFirst("empresaId")?.Value, out var empresaToken))
                return Forbid();

            var vinculoValido = await _db.UsuariosEmpresas.AsNoTracking().AnyAsync(v =>
                v.UsuarioId == usuarioId && v.EmpresaId == empresaToken &&
                v.Ativo && v.Perfil == "Administrador" && v.Empresa.Ativo,
                cancellationToken);
            if (!vinculoValido || (empresaId.HasValue && empresaId.Value != empresaToken))
                return Forbid();
            empresaPermitida = empresaToken;
        }

        var consulta = _db.LogsFiscais.AsNoTracking().AsQueryable();
        if (empresaPermitida.HasValue) consulta = consulta.Where(x => x.EmpresaId == empresaPermitida.Value);
        else if (empresaId.HasValue) consulta = consulta.Where(x => x.EmpresaId == empresaId.Value);
        if (notaFiscalId.HasValue) consulta = consulta.Where(x => x.NotaFiscalId == notaFiscalId.Value);
        if (numero.HasValue) consulta = consulta.Where(x => x.Numero == numero.Value);
        if (inicio.HasValue) consulta = consulta.Where(x => x.CriadoEm >= inicio.Value);
        if (fim.HasValue) consulta = consulta.Where(x => x.CriadoEm <= fim.Value);
        if (!string.IsNullOrWhiteSpace(evento)) consulta = consulta.Where(x => x.Evento == evento.Trim());
        if (cStat.HasValue) consulta = consulta.Where(x => x.CStat == cStat.Value);

        var total = await consulta.CountAsync(cancellationToken);
        var registros = await consulta.OrderByDescending(x => x.CriadoEm).ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina)
            .Select(x => new {
                x.Id, x.EmpresaId, x.NotaFiscalId, x.VendaId, x.Evento, x.Nivel,
                x.Mensagem, x.ChaveAcesso, x.Numero, x.Serie, x.CStat,
                x.Protocolo, x.CriadoEm
            }).ToListAsync(cancellationToken);
        return Ok(new { total, pagina, tamanhoPagina, registros });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhe(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();
        var usuario = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new { u.Ativo, u.AdministradorGeral })
            .FirstOrDefaultAsync(cancellationToken);
        if (usuario == null || !usuario.Ativo) return Forbid();

        var consulta = _db.LogsFiscais.AsNoTracking().Where(x => x.Id == id);
        if (!usuario.AdministradorGeral)
        {
            if (!User.IsInRole("Administrador") ||
                !Guid.TryParse(User.FindFirst("empresaId")?.Value, out var empresaToken))
                return Forbid();
            var vinculoValido = await _db.UsuariosEmpresas.AsNoTracking().AnyAsync(v =>
                v.UsuarioId == usuarioId && v.EmpresaId == empresaToken &&
                v.Ativo && v.Perfil == "Administrador" && v.Empresa.Ativo,
                cancellationToken);
            if (!vinculoValido) return Forbid();
            consulta = consulta.Where(x => x.EmpresaId == empresaToken);
        }
        var log = await consulta.FirstOrDefaultAsync(cancellationToken);
        return log == null ? NotFound() : Ok(log);
    }
}
