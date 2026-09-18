using Fiscal.API.DTOs.Auth;
using Fiscal.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fiscal.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
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
}