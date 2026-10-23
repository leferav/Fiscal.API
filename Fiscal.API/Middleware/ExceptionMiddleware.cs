
using System.Net;
using Fiscal.API.Constants;
using Fiscal.API.DTOs.Common;

namespace Fiscal.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro não tratado na requisição {Path}. TraceId: {TraceId}",
                context.Request.Path,
                context.TraceIdentifier
            );

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode =
                (int)HttpStatusCode.InternalServerError;

            context.Response.ContentType =
                "application/json";

            var resposta = new ErroResponse
            {
                Codigo = "ERRO_INTERNO",
                Mensagem = MensagensErro.ErroInterno,
                TraceId = context.TraceIdentifier
            };

            await context.Response.WriteAsJsonAsync(resposta);
        }
    }
}

