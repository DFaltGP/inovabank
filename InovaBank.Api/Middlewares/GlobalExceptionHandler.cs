using System.Text.Json;
using InovaBank.Domain.Primitives;
using Microsoft.AspNetCore.Diagnostics;
using Polly.CircuitBreaker;

namespace InovaBank.Api.Middlewares;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
            return false;

        var (statusCode, message) = exception switch
        {
            BrokenCircuitException => (
                StatusCodes.Status503ServiceUnavailable,
                "O serviço de validação cadastral externa está temporariamente indisponível. Tente novamente em instantes."
            ),
            HttpRequestException => (
                StatusCodes.Status502BadGateway,
                "Falha na comunicação com os serviços externos de validação."
            ),
            TaskCanceledException or OperationCanceledException => (
                StatusCodes.Status504GatewayTimeout,
                "Tempo limite esgotado ao consultar os serviços externos."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Ocorreu um erro interno inesperado no servidor."
            )
        };

        logger.LogError(
            exception,
            "Falha capturada no pipeline HTTP [{StatusCode}]: {Message} | TraceId: {TraceId}",
            statusCode,
            exception.Message,
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Success = false,
            Data = null,
            Error = message
        };

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            CancellationToken.None);

        return true;
    }
}
