
using Fiscal.API.Data;
using Fiscal.API.Models;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.Requests.Cadastros;
using Fiscal.API.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Fiscal.API.Controllers.Master;

[ApiController]
[Authorize(Policy = "SomenteMaster")]
[Route("api/master/empresas")]
public class MasterEmpresasController : ControllerBase
{
    private readonly FiscalDbContext _context;
    private readonly EmailService _emailService;
    private readonly IConfiguration _configuration;

    public MasterEmpresasController(FiscalDbContext context, EmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration; 
    }

    [HttpGet]
    public async Task<IActionResult> ListarEmpresas()
    {
        var usuarioIdTexto =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdTexto, out var usuarioId))
            return Unauthorized();

        // Revalida a permissão no banco.
        var masterAtivo = await _context.Usuarios
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == usuarioId &&
                x.Ativo &&
                x.AdministradorGeral);

        if (!masterAtivo)
            return Forbid();

        var empresas = await _context.Empresas
            .AsNoTracking()
            .OrderBy(x => x.RazaoSocial)
            .Select(x => new
            {
                x.Id,
                x.Cnpj,
                x.RazaoSocial,
                x.NomeFantasia,
                x.Ativo,
                x.CriadoEm
            })
            .ToListAsync();

        return Ok(empresas);
    }


    [HttpPost]
    public async Task<IActionResult> CadastrarEmpresa( [FromBody] CadastrarEmpresaRequest request)
    {
        var usuarioIdTexto =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdTexto, out var usuarioId))
            return Unauthorized();

        var masterAtivo = await _context.Usuarios
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == usuarioId &&
                x.Ativo &&
                x.AdministradorGeral);

        if (!masterAtivo)
            return Forbid();

        if (request == null ||
            string.IsNullOrWhiteSpace(request.Cnpj) ||
            string.IsNullOrWhiteSpace(request.RazaoSocial))
        {
            return BadRequest(new
            {
                mensagem = "Informe o CNPJ e a razão social."
            });
        }

        var cnpj = new string(
            request.Cnpj.Where(char.IsDigit).ToArray());

        if (cnpj.Length != 14)
        {
            return BadRequest(new
            {
                mensagem = "O CNPJ deve conter 14 dígitos."
            });
        }

        var existe = await _context.Empresas
            .AnyAsync(x => x.Cnpj == cnpj);

        if (existe)
        {
            return Conflict(new
            {
                mensagem = "Já existe uma empresa cadastrada com este CNPJ."
            });
        }

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            Cnpj = cnpj,
            RazaoSocial = request.RazaoSocial.Trim(),
            NomeFantasia = request.NomeFantasia?.Trim(),
            InscricaoEstadual = request.InscricaoEstadual?.Trim(),
            Logradouro = request.Logradouro?.Trim() ?? string.Empty,
            Numero = request.Numero?.Trim() ?? string.Empty,
            Bairro = request.Bairro?.Trim() ?? string.Empty,
            CodigoMunicipio = request.CodigoMunicipio,
            Municipio = request.Municipio?.Trim() ?? string.Empty,
            Uf = request.Uf?.Trim().ToUpperInvariant() ?? string.Empty,
            Cep = new string(
                (request.Cep ?? string.Empty)
                    .Where(char.IsDigit).ToArray()),
            Crt = request.Crt,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(ListarEmpresas),
            new { id = empresa.Id },
            new
            {
                empresa.Id,
                empresa.Cnpj,
                empresa.RazaoSocial,
                empresa.NomeFantasia,
                empresa.Ativo
            });
    }


    [HttpPost("{empresaId:guid}/administrador")]
    public async Task<IActionResult> ConvidarAdministrador( Guid empresaId, [FromBody] CadastrarAdministradorRequest request)
    {
        // 1. Identificar o usuário autenticado
        var usuarioIdTexto =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdTexto, out var usuarioId))
            return Unauthorized();

        // 2. Confirmar que continua sendo Master
        var masterAtivo = await _context.Usuarios
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == usuarioId &&
                x.Ativo &&
                x.AdministradorGeral);

        if (!masterAtivo)
            return Forbid();

        // 3. Verificar a empresa
        var empresa = await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == empresaId && x.Ativo);

        if (empresa == null)
            return NotFound(new
            {
                mensagem = "Empresa não encontrada ou inativa."
            });

        // 4. Validar os dados
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Nome) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                mensagem = "Informe o nome e o e-mail."
            });
        }

        var nome = request.Nome.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        // 5. Evitar duplicação de usuários
        var usuarioExistente = await _context.Usuarios
            .AsNoTracking()
            .AnyAsync(x => x.Email.ToLower() == email);

        if (usuarioExistente)
            return Conflict(new
            {
                mensagem = "Já existe um usuário com este e-mail."
            });

        // 6. Gerar token criptograficamente seguro
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes);

        // 7. Armazenar somente o hash do token
        var hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token));

        var tokenHash = Convert.ToHexString(hashBytes);

        // 8. Invalidar convites anteriores pendentes
        var agora = DateTime.UtcNow;

        var convitesAnteriores =
            await _context.ConvitesUsuarios
                .Where(x =>
                    x.EmpresaId == empresaId &&
                    x.Email.ToLower() == email &&
                    x.UtilizadoEm == null &&
                    x.ExpiraEm > agora)
                .ToListAsync();

        foreach (var conviteAnterior in convitesAnteriores)
            conviteAnterior.ExpiraEm = agora;

        // 9. Criar o convite
        var convite = new ConviteUsuario
        {
            EmpresaId = empresaId,
            Nome = nome,
            Email = email,
            Perfil = "Administrador",
            TokenHash = tokenHash,
            CriadoEm = agora,
            ExpiraEm = agora.AddHours(24)
        };


        var frontendUrl =
            _configuration["Frontend:BaseUrl"]?.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            return StatusCode(500, new
            {
                mensagem = "URL do frontend não configurada."
            });
        }

        var linkConvite =
            $"{frontendUrl}/aceitar-convite?token=" +
            Uri.EscapeDataString(token);

        var nomeSeguro =
            System.Net.WebUtility.HtmlEncode(nome);

        var linkSeguro =
            System.Net.WebUtility.HtmlEncode(linkConvite);

        var conteudoHtml = $"""
                <h2>Bem-vindo ao Fiscal.API!</h2>

                <p>Olá, {nomeSeguro}!</p>

                <p>Você foi convidado para administrar
                uma empresa no Fiscal.API.</p>

                <p>
                    <a href="{linkSeguro}">
                        Clique aqui para definir sua senha
                    </a>
                </p>

                <p>Este convite é válido por 24 horas.</p>

                <p>Se você não esperava este convite,
                desconsidere esta mensagem.</p>
                """;

        _context.ConvitesUsuarios.Add(convite);

        try
        {
            // Primeiro envia o e-mail.
            await _emailService.EnviarEmailAsync(
                email,
                nome,
                "Convite de acesso ao Fiscal.API",
                conteudoHtml);

            // Depois registra o convite no banco.
            await _context.SaveChangesAsync();
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new
            {
                mensagem =
                    "Não foi possível enviar o convite pelo Brevo."
            });
        }

        return Ok(new
        {
            mensagem = "Convite enviado com sucesso.",
            convite.Id,
            convite.EmpresaId,
            convite.Email,
            convite.Perfil,
            convite.ExpiraEm
        });

    }


}
