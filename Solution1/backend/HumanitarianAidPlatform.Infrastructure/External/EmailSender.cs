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
        var mode = _configuration["Notifications:Providers:Email:Mode"];

        _logger.LogInformation(
            "Email notification attempt for recipient {RecipientMask}, correlation {CorrelationId}. Provider enabled: {Enabled}, mode: {Mode}",
            RecipientMasking.Mask(recipient),
            correlationId,
            enabled,
            mode ?? "(missing)");

        if (!enabled)
        {
            _logger.LogWarning(
                "Email notification provider is disabled for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);
            return Failure(
                attemptedAt,
                correlationId,
                "EMAIL_PROVIDER_DISABLED",
                "Email delivery is disabled. Enable the provider and select Sandbox or Real mode.");
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
                "Email notification provider has an invalid mode for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);
            return Failure(
                attemptedAt,
                correlationId,
                "EMAIL_PROVIDER_MODE_INVALID",
                "Email provider mode must be explicitly configured as Sandbox or Real.");
        }

        if (isSandbox)
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
                "Sandbox mode: this is a simulated email delivery for testing or demos; no email was sent.",
                correlationId,
                1,
                null);
        }

        try
        {
            await Task.Delay(1, cancellationToken);
            _logger.LogError(
                "Email provider mode is Real, but no email SDK or credentials are configured for recipient {RecipientMask}, correlation {CorrelationId}",
                RecipientMasking.Mask(recipient),
                correlationId);

            return Failure(
                attemptedAt,
                correlationId,
                "EMAIL_PROVIDER_NOT_IMPLEMENTED",
                "Real email delivery is unavailable because no provider SDK or credentials are configured.");
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