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
        var mode = _configuration["Notifications:Providers:Sms:Mode"];

        _logger.LogInformation(
            "SMS notification attempt for recipient {RecipientMask}, correlation {CorrelationId}. Provider enabled: {Enabled}, mode: {Mode}",
            RecipientMasking.Mask(recipient),
            correlationId,
            enabled,
            mode ?? "(missing)");

        if (!enabled)
        {
            _logger.LogWarning(
                "SMS notification provider is disabled for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);
            return Failure(
                attemptedAt,
                correlationId,
                "SMS_PROVIDER_DISABLED",
                "SMS delivery is disabled. Enable the provider and select Sandbox or Real mode.");
        }

        var isSandbox = string.Equals(
            mode,
            "Sandbox",
            StringComparison.OrdinalIgnoreCase);
        var isReal = string.Equals(
            mode,
            "Real",
            StringComparison.OrdinalIgnoreCase);
        if (!isSandbox && !isReal)
        {
            _logger.LogError(
                "SMS notification provider has an invalid mode for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);
            return Failure(
                attemptedAt,
                correlationId,
                "SMS_PROVIDER_MODE_INVALID",
                "SMS provider mode must be explicitly configured as Sandbox or Real.");
        }

        if (isSandbox)
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
                "Sandbox mode: this is a simulated SMS delivery for testing or demos; no SMS was sent.",
                correlationId,
                1,
                null);
        }

        try
        {
            await Task.Delay(1, cancellationToken);
            _logger.LogError(
                "SMS provider mode is Real, but no SMS SDK or credentials are configured for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);

            return Failure(
                attemptedAt,
                correlationId,
                "SMS_PROVIDER_NOT_IMPLEMENTED",
                "Real SMS delivery is unavailable because no provider SDK or credentials are configured.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "SMS notification failed for recipient {RecipientMask} with {ErrorCode} ({ExceptionType}), correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                "SMS_DELIVERY_FAILED",
                exception.GetType().Name,
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
}