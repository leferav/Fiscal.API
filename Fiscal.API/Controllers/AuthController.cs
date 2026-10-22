using Fiscal.API.DTOs.Auth;
using Fiscal.API.Services;
using Fiscal.API.Services.Email;
using Microsoft.AspNetCore.Mvc;

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

        if (resultado == null)
        {
            return Unauthorized(new
            {
                mensagem = "E-mail ou senha inválidos."
            });
        }

        return Ok(resultado);
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