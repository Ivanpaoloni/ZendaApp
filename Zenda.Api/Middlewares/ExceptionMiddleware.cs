using System.Net;
using System.Text.Json;
using Zenda.Core.DTOs;

namespace Zenda.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // Sigue el flujo normal
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message); // Logueamos el error para nosotros
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        // Decidimos el código de estado según el tipo de excepción
        var statusCode = ex switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest, // Errores de validación (solapamiento, etc)
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError // Errores inesperados
        };

        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = ex.Message,
            // Solo mostramos el StackTrace si estamos en desarrollo
            Details = _env.IsDevelopment() ? ex.StackTrace : null
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
    }
}