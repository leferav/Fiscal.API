using Fiscal.API.Constants;
using Fiscal.API.DTOs.Auth;
using Fiscal.API.DTOs.Common;
using Fiscal.API.Services;
using Fiscal.API.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Fiscal.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ConviteUsuarioService _conviteUsuarioService;

    public AuthController(AuthService authService, ConviteUsuarioService conviteUsuarioService)
    {
        _authService = authService;
        _conviteUsuarioService = conviteUsuarioService; 
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login( [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Senha))
        {
            return BadRequest(new
            {
                mensagem = "Informe o e-mail e a senha."
            });
        }

        var resultado = await _authService.LoginAsync(
            request.Email.Trim(),
            request.Senha
        );


        switch (resultado.Status)
        {
            case StatusLogin.CredenciaisInvalidas:
                return Unauthorized(new ErroResponse
                {
                    Codigo = "CREDENCIAIS_INVALIDAS",
                    Mensagem = MensagensErro.CredenciaisInvalidas,
                    TraceId = HttpContext.TraceIdentifier
                });

            case StatusLogin.UsuarioInativo:
                return StatusCode(403, new ErroResponse
                {
                    Codigo = "USUARIO_INATIVO",
                    Mensagem = MensagensErro.UsuarioInativo,
                    TraceId = HttpContext.TraceIdentifier
                });

            case StatusLogin.MultiplasEmpresas:
                return Conflict(new ErroResponse
                {
                    Codigo = "MULTIPLAS_EMPRESAS",
                    Mensagem = MensagensErro.MultiplasEmpresas,
                    TraceId = HttpContext.TraceIdentifier
                });

            case StatusLogin.Sucesso:
                if (resultado.Dados == null)
                    return StatusCode(500);

                return Ok(resultado.Dados);

            default:
                return StatusCode(500);
        }

    }


    [Authorize(Roles = "Master")]
    [HttpPost("selecionar-empresa")]
    public async Task<IActionResult> SelecionarEmpresa([FromBody] SelecionarEmpresaRequest request)
    {
        if (request == null || request.EmpresaId == Guid.Empty)
        {
            return BadRequest(new
            {
                mensagem = "Informe a empresa."
            });
        }

        var usuarioIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
        {
            return Unauthorized(new
            {
                mensagem = "Usuário não identificado."
            });
        }

        var tokenAtual = Request.Headers.Authorization
            .ToString()
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        var jwt = new JwtSecurityTokenHandler()
            .ReadJwtToken(tokenAtual);

        var expiraEm = jwt.ValidTo;

        var resultado = await _authService
            .GerarTokenMasterEmpresaAsync(
                usuarioId,
                request.EmpresaId,
                expiraEm);

        if (resultado == null)
        {
            return StatusCode(403, new
            {
                mensagem = "Usuário Master ou empresa inválida."
            });
        }

        return Ok(new
        {
            token = resultado.Value.Token,
            expiraEm = resultado.Value.ExpiraEm,
            empresaId = request.EmpresaId
        });
    }


    [HttpPost("aceitar-convite")]
    public async Task<IActionResult> AceitarConvite( [FromBody] AceitarConviteRequest request)
    {
        if (request == null)
        {
            return BadRequest(new
            {
                mensagem = "Informe os dados do convite."
            });
        }

        var resultado =
            await _conviteUsuarioService.AceitarAsync(request);

        if (!resultado.Sucesso)
        {
            return BadRequest(new
            {
                mensagem = resultado.Mensagem
            });
        }

        return Ok(new
        {
            mensagem = resultado.Mensagem
        });
    }

}

public class SelecionarEmpresaRequest
{
    public Guid EmpresaId { get; set; }
}