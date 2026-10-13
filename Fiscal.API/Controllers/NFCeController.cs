using Fiscal.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Fiscal.API.Models.NFe;
using Fiscal.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fiscal.API.Controllers
{
    [ApiController]
    [Route("api/nfce")]
    public class NFCeController : ControllerBase
    {
        private readonly FiscalService _fiscalService;
        private readonly FiscalDbContext _context;

        public NFCeController(FiscalService fiscalService, FiscalDbContext context)
        {
            _fiscalService = fiscalService;
            _context = context;
        }

        [HttpPost("autorizar")]
        public async Task<IActionResult> Autorizar([FromBody] EmitirNFeRequest request)
        {
            try
            {
                var resultado =
                    await _fiscalService.AutorizarNFCeAsync(request);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    erro = ex.Message,
                    detalhe = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // Rota separada para testar a emissão pelo Fiscal.Agent.
        // A rota original api/nfce/autorizar continua funcionando normalmente.
        [HttpPost("autorizar-agent")]
        public async Task<IActionResult> AutorizarPeloAgent([FromBody] EmitirNFeRequest request)
        {
            try
            {
                var resultado = await _fiscalService.AutorizarNFCeAsync(request, usarAgent: true);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    erro = ex.Message,
                    detalhe = ex.InnerException?.Message
                });
            }
        }


        // Consulta segura da solicitação criada pelo endpoint autorizar-agent.
        // O solicitacaoId deve ser o Id da NotaFiscal gerada na fila.
        [Authorize]
        [HttpGet("solicitacoes/{solicitacaoId:guid}")]
        public async Task<IActionResult> ConsultarSolicitacao(Guid solicitacaoId)
        {
            var empresaClaim = User.FindFirst("empresaId")?.Value;
            if (!Guid.TryParse(empresaClaim, out var empresaId))
                return Forbid();

            var nota = await _context.NotasFiscais
                .AsNoTracking()
                .Where(n => n.Id == solicitacaoId && n.EmpresaId == empresaId && n.Modelo == 65)
                .Select(n => new
                {
                    solicitacaoId = n.Id,
                    numero = n.Numero,
                    serie = n.Serie,
                    status = n.Status,
                    cStat = n.CStat,
                    xMotivo = n.XMotivo,
                    protocolo = n.Protocolo,
                    chaveAcesso = n.ChaveAcesso,
                    atualizadoEm = n.AtualizadoEm
                })
                .FirstOrDefaultAsync();

            if (nota == null)
                return NotFound(new { mensagem = "Solicitação não encontrada." });

            return Ok(nota);
        }

        [HttpGet("consultar/{chave}")]
        public IActionResult Consultar(string chave)
        {
            try
            {
                var resultado =
                    _fiscalService.ConsultarNFCe(chave);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    erro = ex.Message,
                    detalhe = ex.InnerException?.Message
                });
            }
        }
    }
}