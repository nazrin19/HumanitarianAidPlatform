using System.Net;
using System.Net.Mail;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HumanitarianAidPlatform.Infrastructure.External;

public sealed class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<NotificationDeliveryResult> SendAsync(
        string recipient,
        string? subject,
        string message,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var attemptedAt = DateTimeOffset.UtcNow;
        var recipientMask = RecipientMasking.Mask(recipient);
        var enabled = !bool.TryParse(
            _configuration["Notifications:Providers:Email:Enabled"],
            out var configuredEnabled) || configuredEnabled;
        if (!enabled)
        {
            return Failure(attemptedAt, correlationId, "EMAIL_PROVIDER_DISABLED",
                "Email delivery is disabled.");
        }

        var mode = _configuration["Notifications:ProviderMode"] ??
            _configuration["Notifications:Providers:Email:Mode"] ?? "Sandbox";
        if (string.Equals(mode, "Sandbox", StringComparison.OrdinalIgnoreCase))
        {
            return SandboxResult(attemptedAt, correlationId, 1, null);
        }
        if (!string.Equals(mode, "Live", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mode, "Real", StringComparison.OrdinalIgnoreCase))
        {
            return Failure(attemptedAt, correlationId, "EMAIL_PROVIDER_MODE_INVALID",
                "Email provider mode must be Sandbox or Live.");
        }

        var host = _configuration["Notifications:Providers:Email:Smtp:Host"];
        var from = _configuration["Notifications:Providers:Email:Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) ||
            !int.TryParse(_configuration["Notifications:Providers:Email:Smtp:Port"], out var port) ||
            port is < 1 or > 65535 ||
            !System.Net.Mail.MailAddress.TryCreate(from, out _))
        {
            _logger.LogWarning(
                "Live email is not configured for recipient {RecipientMask}, correlation {CorrelationId}; using Sandbox.",
                recipientMask,
                correlationId);
            return SandboxResult(attemptedAt, correlationId, 0,
                "SMTP is not configured; no email was sent.");
        }

        var timeoutSeconds = ReadBoundedSetting(
            "Notifications:Providers:Email:TimeoutSeconds", 10, 1, 60);
        var retries = ReadBoundedSetting(
            "Notifications:Providers:Email:MaximumRetries", 2, 0, 3);
        var username = _configuration["Notifications:Providers:Email:Smtp:Username"];
        var password = _configuration["Notifications:Providers:Email:Smtp:Password"];
        var enableSsl = !bool.TryParse(
            _configuration["Notifications:Providers:Email:Smtp:EnableSsl"],
            out var configuredSsl) || configuredSsl;

        for (var attempt = 1; attempt <= retries + 1; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                using var mail = new MailMessage(from!, recipient, subject ?? "Aid update", message);
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Timeout = timeoutSeconds * 1000
                };
                if (!string.IsNullOrWhiteSpace(username))
                {
                    client.Credentials = new NetworkCredential(username, password ?? string.Empty);
                }

                await client.SendMailAsync(mail, timeoutSource.Token);
                return new NotificationDeliveryResult(
                    true,
                    NotificationDeliveryStatus.Sent,
                    NotificationChannel.Email,
                    attemptedAt,
                    DateTimeOffset.UtcNow,
                    null,
                    null,
                    correlationId,
                    attempt,
                    null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is SmtpException or OperationCanceledException or TimeoutException)
            {
                _logger.LogWarning(
                    "SMTP attempt {AttemptCount} failed for recipient {RecipientMask} ({ExceptionType}), correlation {CorrelationId}.",
                    attempt,
                    recipientMask,
                    exception.GetType().Name,
                    correlationId);
                if (attempt < retries + 1)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
                }
            }
        }

        _logger.LogWarning(
            "Live email delivery failed after bounded retries for recipient {RecipientMask}, correlation {CorrelationId}; using Sandbox.",
            recipientMask,
            correlationId);
        return SandboxResult(attemptedAt, correlationId, retries + 1,
            "Live email could not be delivered; Sandbox fallback was recorded instead.");
    }

    private NotificationDeliveryResult SandboxResult(
        DateTimeOffset attemptedAt,
        Guid correlationId,
        int attempts,
        string? detail)
    {
        _logger.LogInformation(
            "Email used Sandbox mode for correlation {CorrelationId}.",
            correlationId);
        var message = detail is null
            ? "Sandbox mode: no email was sent."
            : $"Sandbox fallback: {detail}";
        return new NotificationDeliveryResult(
            true,
            NotificationDeliveryStatus.MockSent,
            NotificationChannel.Email,
            attemptedAt,
            DateTimeOffset.UtcNow,
            null,
            message,
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

    private static NotificationDeliveryResult Failure(
        DateTimeOffset attemptedAt,
        Guid correlationId,
        string errorCode,
        string errorMessage) =>
        new(
            false,
            NotificationDeliveryStatus.Failed,
            NotificationChannel.Email,
            attemptedAt,
            DateTimeOffset.UtcNow,
            errorCode,
            errorMessage,
            correlationId,
            1,
            null);
}
