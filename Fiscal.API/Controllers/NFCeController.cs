using Fiscal.API.Data;
using Fiscal.API.Models.NFe;
using Fiscal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Controllers
{
    [ApiController]
    [Authorize]
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
        [Authorize(Roles = "Master,Administrador,Operador")]
        public async Task<IActionResult> Autorizar([FromBody] EmitirNFeRequest request)
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (!Guid.TryParse(empresaIdClaim, out var empresaId))
            {
                return Unauthorized(new
                {
                    mensagem = "Empresa não identificada no token."
                });
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    mensagem = "Dados da NFC-e não informados."
                });
            }

            if (request.EmpresaId != empresaId)
            {
                return StatusCode(403, new
                {
                    mensagem = "Você não tem permissão para emitir NFC-e para esta empresa."
                });
            }

            var certificado = await _context.CertificadosDigitais
                .AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.Ativo)
                .Select(x => new
                {
                    x.ValidoAte,
                    x.ReferenciaArmazenamento
                })
                .FirstOrDefaultAsync();

            if (certificado == null ||
                string.IsNullOrWhiteSpace(certificado.ReferenciaArmazenamento))
            {
                return UnprocessableEntity(new
                {
                    codigo = "CERTIFICADO_NAO_CONFIGURADO",
                    mensagem = "Certificado digital não configurado para esta empresa. Acesse Configurações > Certificado Digital para cadastrar um certificado A1 válido."
                });
            }

            if (certificado.ValidoAte < DateTime.UtcNow)
            {
                return UnprocessableEntity(new
                {
                    codigo = "CERTIFICADO_VENCIDO",
                    mensagem = $"O certificado digital da empresa venceu em {certificado.ValidoAte:dd/MM/yyyy}. Acesse Configurações > Certificado Digital para renová-lo."
                });
            }

            try
            {
                var resultado =
                    await _fiscalService.AutorizarNFCeAsync(request);

                return Ok(resultado);
            }
            catch (NotSupportedException ex) when (
                ex.Message.StartsWith("CST ICMS ", StringComparison.OrdinalIgnoreCase) &&
                ex.Message.EndsWith(" ainda não implementado.", StringComparison.OrdinalIgnoreCase))
            {
                // Erro fiscal conhecido e seguro para exibir ao operador.
                return UnprocessableEntity(new
                {
                    codigo = "CST_ICMS_NAO_IMPLEMENTADO",
                    mensagem = ex.Message
                });
            }
            catch (Exception ex)
            {
                // O builder pode lançar Exception genérica para CST ainda não suportado.
                // Somente esta mensagem conhecida é devolvida ao frontend.
                if (System.Text.RegularExpressions.Regex.IsMatch(
                    ex.Message,
                    @"^CST ICMS [0-9]{2,3} ainda não implementado\.$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return UnprocessableEntity(new
                    {
                        codigo = "CST_ICMS_NAO_IMPLEMENTADO",
                        mensagem = ex.Message
                    });
                }

                System.Diagnostics.Debug.WriteLine(
                    "ERRO EMISSÃO NFC-e: " + ex.ToString());

                System.Diagnostics.Debug.WriteLine(
                    "ERRO INTERNO: " + ex.GetBaseException().ToString());

                System.Diagnostics.Debug.WriteLine(
                    "ERRO COMPLETO: " + ex.ToString());

                System.Diagnostics.Debug.WriteLine(
                    "CAUSA INTERNA: " + ex.GetBaseException().ToString());

                return StatusCode(500, new
                {
                    mensagem = "Não foi possível processar a emissão da NFC-e."
                });
            }
        }


        [HttpGet("consultar/{chave}")]
        public async Task<IActionResult> Consultar(string chave)
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (!Guid.TryParse(empresaIdClaim, out var empresaId))
            {
                return Unauthorized(new
                {
                    mensagem = "Empresa não identificada no token."
                });
            }

            try
            {
                var resultado = await _fiscalService
                    .ConsultarNFCeAsync(chave, empresaId);

                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "ERRO CONSULTA NFC-e: " + ex);

                return StatusCode(500, new
                {
                    mensagem = "Não foi possível consultar a NFC-e na SEFAZ."
                });
            }
        }


        // GET api/NFCe/pendencias
        [HttpGet("pendencias")]
        [Authorize]
        public async Task<IActionResult> ConsultarPendencias()
        {
            var empresaIdClaim = User.FindFirst("empresaId")?.Value;

            if (!Guid.TryParse(empresaIdClaim, out var empresaId))
            {
                return Unauthorized(new
                {
                    mensagem = "Empresa não identificada no token."
                });
            }

            var resultado = await _fiscalService
                .ConsultarPendenciasNFCeAsync(empresaId);

            return Ok(resultado);
        }
    }
}