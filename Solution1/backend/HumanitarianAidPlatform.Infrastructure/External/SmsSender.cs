using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HumanitarianAidPlatform.Infrastructure.External;

public sealed class SmsSender : ISmsSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(IConfiguration configuration, ILogger<SmsSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<NotificationDeliveryResult> SendAsync(
        string recipient,
        string message,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var attemptedAt = DateTimeOffset.UtcNow;
        cancellationToken.ThrowIfCancellationRequested();

        var enabled = bool.TryParse(
            _configuration["Notifications:Providers:Sms:Enabled"],
            out var isEnabled) && isEnabled;
        var mode = _configuration["Notifications:Providers:Sms:Mode"] ?? "Sandbox";
        var sandbox = !enabled ||
            !string.Equals(mode, "Real", StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation(
            "SMS notification attempt for recipient {RecipientMask}, correlation {CorrelationId}. Real provider enabled: {Enabled}",
            RecipientMasking.Mask(recipient),
            correlationId,
            enabled);

        if (sandbox)
        {
            await Task.Delay(1, cancellationToken);
            _logger.LogWarning(
                "SMS notification used sandbox/mock delivery for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);

            return new NotificationDeliveryResult(
                true,
                NotificationDeliveryStatus.MockSent,
                NotificationChannel.Sms,
                attemptedAt,
                DateTimeOffset.UtcNow,
                null,
                "Sandbox mode: real SMS provider is not configured.",
                correlationId,
                1,
                null);
        }

        try
        {
            await Task.Delay(1, cancellationToken);
            _logger.LogError(
                "SMS provider mode is Real, but no real SMS SDK is configured for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);

            return new NotificationDeliveryResult(
                false,
                NotificationDeliveryStatus.Failed,
                NotificationChannel.Sms,
                attemptedAt,
                DateTimeOffset.UtcNow,
                "SMS_PROVIDER_NOT_IMPLEMENTED",
                "Real SMS delivery is not implemented for this provider mode.",
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
                exception,
                "SMS notification failed for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);

            return new NotificationDeliveryResult(
                false,
                NotificationDeliveryStatus.Failed,
                NotificationChannel.Sms,
                attemptedAt,
                DateTimeOffset.UtcNow,
                "SMS_DELIVERY_FAILED",
                "The SMS provider could not deliver the notification.",
                correlationId,
                1,
                null);
        }
    }
}