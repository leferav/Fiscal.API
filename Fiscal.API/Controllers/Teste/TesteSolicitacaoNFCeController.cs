using Fiscal.API.Models.NFe;
using Fiscal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fiscal.API.Controllers.Dev;

[ApiController]
[Route("api/dev/teste-solicitacao-nfce")]
[Authorize]
public class TesteSolicitacaoNFCeController : ControllerBase
{
    private readonly FiscalService _fiscalService;
    private readonly IWebHostEnvironment _environment;

    public TesteSolicitacaoNFCeController(
        FiscalService fiscalService,
        IWebHostEnvironment environment)
    {
        _fiscalService = fiscalService;
        _environment = environment;
    }

    [HttpPost]
    public async Task<IActionResult> Testar(
        [FromBody] EmitirNFeRequest request)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var resultado =
            await _fiscalService.TestarSolicitacaoNFCeAsync(request);

        return Ok(resultado);
    }
}