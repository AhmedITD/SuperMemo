using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Domain.Exceptions;

namespace SuperMemo.Infrastructure.Services;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message, errors, code) = MapException(exception);

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);

        var response = ApiResponse<object>.ErrorResponse(message, errors, code);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, jsonOptions),
            cancellationToken);
        return true;
    }

    private (int StatusCode, string Message, Dictionary<string, string[]>? Errors, string? Code) MapException(Exception ex)
    {
        return ex switch
        {
            ValidationException v => (
                400,
                string.IsNullOrEmpty(v.Message) ? "Validation failed." : v.Message,
                v.Errors
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray()),
                "VALIDATION_FAILED"),
            DomainException d => (400, d.Message, null, d.Code),
            UnauthorizedAccessException => (401, string.IsNullOrEmpty(ex.Message) ? "Unauthorized." : ex.Message, null, null),
            KeyNotFoundException => (404, string.IsNullOrEmpty(ex.Message) ? "Resource not found." : ex.Message, null, "RESOURCE_NOT_FOUND"),
            ArgumentException => (400, ex.Message, null, null),
            _ => _env.IsDevelopment()
                ? (500, ex.Message ?? "An error occurred.", null, null)
                : (500, "An internal error occurred.", null, null)
        };
    }
}
