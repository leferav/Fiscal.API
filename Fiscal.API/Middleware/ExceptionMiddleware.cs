using System.Net;
using Fiscal.API.Constants;
using Fiscal.API.DTOs.Common;
using Fiscal.API.Exceptions;
using Fiscal.API.Models.Database;
using Fiscal.API.Services;

namespace Fiscal.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, HistoricoFiscalService historico)
    {
        try
        {
            await _next(context);
        }
        catch (ValidacaoFiscalException ex)
        {
            if (context.Response.HasStarted)
                throw;

            _logger.LogWarning(ex,
                "Validação fiscal na requisição {Path}. TraceId: {TraceId}",
                context.Request.Path, context.TraceIdentifier);

            await RegistrarErroAsync(context, historico, "VALIDACAO_FISCAL", "Warning", ex.Message);

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ErroResponse
            {
                Codigo = "VALIDACAO_FISCAL",
                Mensagem = ex.Message,
                TraceId = context.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
                throw;

            _logger.LogError(ex,
                "Erro não tratado na requisição {Path}. TraceId: {TraceId}",
                context.Request.Path, context.TraceIdentifier);

            // Não persistir ex.ToString() ou dados da requisição: podem conter segredos.
            await RegistrarErroAsync(context, historico, "ERRO_INTERNO", "Error", MensagensErro.ErroInterno);

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErroResponse
            {
                Codigo = "ERRO_INTERNO",
                Mensagem = MensagensErro.ErroInterno,
                TraceId = context.TraceIdentifier
            });
        }
    }

    private async Task RegistrarErroAsync(
        HttpContext context,
        HistoricoFiscalService historico,
        string evento,
        string nivel,
        string mensagem)
    {
        // Sem empresa autenticada, registrar somente no logger da aplicação.
        var empresaIdTexto = context.User.FindFirst("empresaId")?.Value;
        if (!Guid.TryParse(empresaIdTexto, out var empresaId) || empresaId == Guid.Empty)
            return;

        try
        {
            await historico.RegistrarAsync(new LogFiscal
            {
                EmpresaId = empresaId,
                Evento = evento,
                Nivel = nivel,
                Mensagem = mensagem,
                DetalhesErro = $"Método: {context.Request.Method}; " +
                               $"Rota: {context.Request.Path}; " +
                               $"TraceId: {context.TraceIdentifier}"
            });
        }
        catch (Exception ex)
        {
            // O histórico nunca pode impedir a resposta ao cliente.
            _logger.LogError(ex, "Falha inesperada ao registrar histórico fiscal. TraceId: {TraceId}",
                context.TraceIdentifier);
        }
    }
}
