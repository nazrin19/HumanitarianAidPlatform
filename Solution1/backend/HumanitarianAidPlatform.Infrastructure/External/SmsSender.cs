using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HumanitarianAidPlatform.Infrastructure.External;

public sealed class SmsSender : ISmsSender, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsSender> _logger;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public SmsSender(IConfiguration configuration, ILogger<SmsSender> logger)
    {
        _httpClient = new HttpClient();
        _ownsHttpClient = true;
        _configuration = configuration;
        _logger = logger;
    }

    public SmsSender(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SmsSender> logger)
    {
        _httpClient = httpClient;
        _ownsHttpClient = false;
        _configuration = configuration;
        _logger = logger;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    public async Task<NotificationDeliveryResult> SendAsync(
        string recipient,
        string message,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var attemptedAt = DateTimeOffset.UtcNow;
        var recipientMask = RecipientMasking.Mask(recipient);
        var enabled = !bool.TryParse(
            _configuration["Notifications:Providers:Sms:Enabled"],
            out var configuredEnabled) || configuredEnabled;
        if (!enabled)
        {
            return Failure(attemptedAt, correlationId, "SMS_PROVIDER_DISABLED",
                "SMS delivery is disabled.");
        }

        var mode = _configuration["Notifications:Providers:Sms:Mode"] ??
            _configuration["Notifications:ProviderMode"] ?? "Sandbox";
        if (string.Equals(mode, "Sandbox", StringComparison.OrdinalIgnoreCase))
        {
            return SandboxResult(attemptedAt, recipientMask, correlationId, 1, null);
        }
        if (!string.Equals(mode, "Live", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mode, "Real", StringComparison.OrdinalIgnoreCase))
        {
            return Failure(attemptedAt, correlationId, "SMS_PROVIDER_MODE_INVALID",
                "SMS provider mode must be Sandbox or Live.");
        }

        var webhookUrl = _configuration["Notifications:Providers:Sms:WebhookUrl"];
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var webhookUri) ||
            webhookUri.Scheme is not ("https" or "http"))
        {
            _logger.LogWarning(
                "Live SMS webhook is not configured for recipient {RecipientMask}, correlation {CorrelationId}; using Sandbox.",
                recipientMask,
                correlationId);
            return SandboxResult(attemptedAt, recipientMask, correlationId, 0,
                "The SMS webhook is not configured; no SMS was sent.");
        }

        var timeoutSeconds = ReadBoundedSetting(
            "Notifications:Providers:Sms:TimeoutSeconds", 10, 1, 60);
        var retries = ReadBoundedSetting(
            "Notifications:Providers:Sms:MaximumRetries", 2, 0, 3);
        var bearerToken = _configuration["Notifications:Providers:Sms:BearerToken"];

        for (var attempt = 1; attempt <= retries + 1; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, webhookUri)
                {
                    Content = JsonContent.Create(new SmsWebhookRequest(
                        recipient,
                        message,
                        correlationId))
                };
                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutSource.Token);
                if (response.IsSuccessStatusCode)
                {
                    return new NotificationDeliveryResult(
                        true,
                        NotificationDeliveryStatus.Sent,
                        NotificationChannel.Sms,
                        attemptedAt,
                        DateTimeOffset.UtcNow,
                        null,
                        null,
                        correlationId,
                        attempt,
                        null);
                }

                if (!IsTransient(response.StatusCode))
                {
                    _logger.LogWarning(
                        "SMS webhook returned HTTP {StatusCode} for recipient {RecipientMask}, correlation {CorrelationId}; using Sandbox.",
                        (int)response.StatusCode,
                        recipientMask,
                        correlationId);
                    return SandboxResult(
                        attemptedAt,
                        recipientMask,
                        correlationId,
                        attempt,
                        $"The SMS webhook returned HTTP {(int)response.StatusCode}; no delivery was confirmed.");
                }

                _logger.LogWarning(
                    "SMS webhook returned transient HTTP {StatusCode} on attempt {AttemptCount} for recipient {RecipientMask}, correlation {CorrelationId}.",
                    (int)response.StatusCode,
                    attempt,
                    recipientMask,
                    correlationId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is HttpRequestException or OperationCanceledException or TimeoutException)
            {
                _logger.LogWarning(
                    "SMS webhook attempt {AttemptCount} failed for recipient {RecipientMask} ({ExceptionType}), correlation {CorrelationId}.",
                    attempt,
                    recipientMask,
                    exception.GetType().Name,
                    correlationId);
            }

            if (attempt < retries + 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }

        _logger.LogWarning(
            "Live SMS delivery failed after bounded retries for recipient {RecipientMask}, correlation {CorrelationId}; using Sandbox.",
            recipientMask,
            correlationId);
        return SandboxResult(attemptedAt, recipientMask, correlationId, retries + 1,
            "Live SMS delivery could not be confirmed; Sandbox fallback was recorded instead.");
    }

    private NotificationDeliveryResult SandboxResult(
        DateTimeOffset attemptedAt,
        string recipientMask,
        Guid correlationId,
        int attempts,
        string? detail)
    {
        _logger.LogInformation(
            "SMS used Sandbox mode for recipient {RecipientMask}, correlation {CorrelationId}.",
            recipientMask,
            correlationId);
        var resultMessage = detail is null
            ? "Sandbox mode: no SMS was sent."
            : $"Sandbox fallback: {detail}";
        return new NotificationDeliveryResult(
            true,
            NotificationDeliveryStatus.MockSent,
            NotificationChannel.Sms,
            attemptedAt,
            DateTimeOffset.UtcNow,
            null,
            resultMessage,
            correlationId,
            Math.Max(1, attempts),
            null);
    }

    private int ReadBoundedSetting(
        string key,
        int defaultValue,
        int minimum,
        int maximum) =>
        int.TryParse(_configuration[key], out var configuredValue)
            ? Math.Clamp(configuredValue, minimum, maximum)
            : defaultValue;

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;

    private static NotificationDeliveryResult Failure(
        DateTimeOffset attemptedAt,
        Guid correlationId,
        string errorCode,
        string errorMessage) =>
        new(
            false,
            NotificationDeliveryStatus.Failed,
            NotificationChannel.Sms,
            attemptedAt,
            DateTimeOffset.UtcNow,
            errorCode,
            errorMessage,
            correlationId,
            1,
            null);

    private sealed record SmsWebhookRequest(
        string To,
        string Message,
        Guid CorrelationId);
}
