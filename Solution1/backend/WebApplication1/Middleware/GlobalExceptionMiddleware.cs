using System.Net;
using System.Text.Json;
using HumanitarianAidPlatform.Application.Notifications;

namespace HumanitarianAidPlatform.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var traceId = context.TraceIdentifier;
            var isNotificationPersistenceFailure =
                exception is NotificationPersistenceException;

            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                traceId);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = isNotificationPersistenceFailure
                ? (int)HttpStatusCode.ServiceUnavailable
                : (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = new
                {
                    code = isNotificationPersistenceFailure
                        ? "notification_persistence_unavailable"
                        : "internal_server_error",
                    message = isNotificationPersistenceFailure
                        ? "Notification persistence is temporarily unavailable."
                        : "An unexpected error occurred.",
                    operation = (exception as NotificationPersistenceException)?.Operation,
                    traceId,
                    detail = _environment.IsDevelopment()
                        ? exception.Message
                        : null
                }
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}
