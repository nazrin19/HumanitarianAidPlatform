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
        cancellationToken.ThrowIfCancellationRequested();

        var enabled = bool.TryParse(
            _configuration["Notifications:Providers:Email:Enabled"],
            out var isEnabled) && isEnabled;
        var mode = _configuration["Notifications:Providers:Email:Mode"] ?? "Sandbox";
        var sandbox = !enabled ||
            !string.Equals(mode, "Real", StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Email notification attempt for recipient {RecipientMask}, correlation {CorrelationId}. Real provider enabled: {Enabled}",
            RecipientMasking.Mask(recipient),
            correlationId,
            enabled);

        if (sandbox)
        {
            await Task.Delay(1, cancellationToken);
            _logger.LogWarning(
                "Email notification used sandbox/mock delivery for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);

            return new NotificationDeliveryResult(
                true,
                NotificationDeliveryStatus.MockSent,
                NotificationChannel.Email,
                attemptedAt,
                DateTimeOffset.UtcNow,
                null,
                "Sandbox mode: real email provider is not configured.",
                correlationId,
                1,
                null);
        }

        try
        {
            await Task.Delay(1, cancellationToken);
            _logger.LogError(
                "Email provider mode is Real, but no real email SDK is configured for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);

            return new NotificationDeliveryResult(
                false,
                NotificationDeliveryStatus.Failed,
                NotificationChannel.Email,
                attemptedAt,
                DateTimeOffset.UtcNow,
                "EMAIL_PROVIDER_NOT_IMPLEMENTED",
                "Real email delivery is not implemented for this provider mode.",
                correlationId,
                1,
                null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Email notification failed for recipient {RecipientMask} with {ErrorCode} ({ExceptionType}), correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                "EMAIL_DELIVERY_FAILED",
                exception.GetType().Name,
                correlationId);

            return new NotificationDeliveryResult(
                false,
                NotificationDeliveryStatus.Failed,
                NotificationChannel.Email,
                attemptedAt,
                DateTimeOffset.UtcNow,
                "EMAIL_DELIVERY_FAILED",
                "The email provider could not deliver the notification.",
                correlationId,
                1,
                null);
        }
    }
}