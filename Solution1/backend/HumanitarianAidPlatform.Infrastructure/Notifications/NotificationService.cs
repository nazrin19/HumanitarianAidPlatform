using System.ComponentModel.DataAnnotations;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Infrastructure.External;
using Microsoft.Extensions.Logging;

namespace HumanitarianAidPlatform.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly NotificationDeliveryDeduplicator _deduplicator;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailSender emailSender,
        ISmsSender smsSender,
        ILogger<NotificationService> logger)
        : this(
            emailSender,
            smsSender,
            new NotificationDeliveryDeduplicator(),
            logger)
    {
    }

    public NotificationService(
        IEmailSender emailSender,
        ISmsSender smsSender,
        NotificationDeliveryDeduplicator deduplicator,
        ILogger<NotificationService> logger)
    {
        _emailSender = emailSender;
        _smsSender = smsSender;
        _deduplicator = deduplicator;
        _logger = logger;
    }

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return InvalidInputResult(
                NotificationChannel.Unknown,
                "The notification request is required.");
        }

        var validationError = GetValidationError(request);
        if (validationError is not null)
        {
            return InvalidInputResult(request.Channel, validationError);
        }

        if (request.EventId is not Guid eventId)
        {
            return await SendCoreAsync(request, cancellationToken);
        }

        try
        {
            var result = await _deduplicator.ExecuteAsync(
                eventId,
                request.Channel,
                request.Recipient,
                () => SendCoreAsync(request, cancellationToken),
                cancellationToken);

            if (result.IsDuplicate)
            {
                _logger.LogInformation(
                    "Duplicate notification event {EventId} suppressed for {Channel}, correlation {CorrelationId}",
                    eventId,
                    request.Channel,
                    result.CorrelationId);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var correlationId = Guid.NewGuid();
            _logger.LogInformation(
                "Duplicate notification wait was cancelled for {Channel}, correlation {CorrelationId}",
                request.Channel,
                correlationId);
            return CancelledResult(request.Channel, correlationId, DateTimeOffset.UtcNow);
        }
    }

    private async Task<NotificationDeliveryResult> SendCoreAsync(
        NotificationRequest request,
        CancellationToken cancellationToken)
    {
        var attemptedAt = DateTimeOffset.UtcNow;
        var correlationId = Guid.NewGuid();
        var recipientMask = RecipientMasking.Mask(request.Recipient);
        _logger.LogInformation(
            "Routing notification through {Channel} for recipient {RecipientMask}, correlation {CorrelationId}",
            request.Channel,
            recipientMask,
            correlationId);

        try
        {
            var result = request.Channel switch
            {
                NotificationChannel.Email => await _emailSender.SendAsync(
                    request.Recipient,
                    request.Subject,
                    request.Message,
                    correlationId,
                    cancellationToken),
                NotificationChannel.Sms => await _smsSender.SendAsync(
                    request.Recipient,
                    request.Message,
                    correlationId,
                    cancellationToken),
                NotificationChannel.InApp => new NotificationDeliveryResult(
                    true,
                    NotificationDeliveryStatus.MockSent,
                    NotificationChannel.InApp,
                    attemptedAt,
                    DateTimeOffset.UtcNow,
                    null,
                    "In-app notification is not persisted; this is an explicit sandbox-only simulation.",
                    correlationId,
                    1,
                    null),
                _ => new NotificationDeliveryResult(
                    false,
                    NotificationDeliveryStatus.Failed,
                    NotificationChannel.Unknown,
                    attemptedAt,
                    DateTimeOffset.UtcNow,
                    "UNSUPPORTED_CHANNEL",
                    "The requested notification channel is not supported.",
                    correlationId,
                    1,
                    null)
            };

            result = result with
            {
                CorrelationId = correlationId,
                AttemptCount = Math.Max(result.AttemptCount, 1)
            };

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Notification delivered with status {DeliveryStatus} through {Channel}, correlation {CorrelationId}",
                    result.Status,
                    result.Channel,
                    correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "Notification failed with status {DeliveryStatus} through {Channel}: {ErrorCode}, correlation {CorrelationId}",
                    result.Status,
                    result.Channel,
                    result.ErrorCode,
                    correlationId);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Notification delivery was cancelled through {Channel}, correlation {CorrelationId}",
                request.Channel,
                correlationId);

            return CancelledResult(request.Channel, correlationId, attemptedAt);
        }
        catch (TimeoutException)
        {
            return ProviderTimeoutResult(request.Channel, correlationId, attemptedAt);
        }
        catch (OperationCanceledException)
        {
            return ProviderTimeoutResult(request.Channel, correlationId, attemptedAt);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Notification delivery failed through {Channel} with {ErrorCode} ({ExceptionType}), correlation {CorrelationId}",
                request.Channel,
                "NOTIFICATION_DELIVERY_ERROR",
                exception.GetType().Name,
                correlationId);

            return new NotificationDeliveryResult(
                false,
                NotificationDeliveryStatus.Failed,
                request.Channel,
                attemptedAt,
                DateTimeOffset.UtcNow,
                "NOTIFICATION_DELIVERY_ERROR",
                "The notification could not be delivered.",
                correlationId,
                1,
                null);
        }
    }

    private static NotificationDeliveryResult CancelledResult(
        NotificationChannel channel,
        Guid correlationId,
        DateTimeOffset attemptedAt) =>
        new(
            false,
            NotificationDeliveryStatus.Cancelled,
            channel,
            attemptedAt,
            DateTimeOffset.UtcNow,
            "NOTIFICATION_CANCELLED",
            "The notification delivery was cancelled.",
            correlationId,
            1,
            null);

    private NotificationDeliveryResult ProviderTimeoutResult(
        NotificationChannel channel,
        Guid correlationId,
        DateTimeOffset attemptedAt)
    {
        _logger.LogWarning(
            "Notification provider failed with {ErrorCode} through {Channel}, correlation {CorrelationId}",
            "NOTIFICATION_TIMEOUT",
            channel,
            correlationId);

        return new NotificationDeliveryResult(
            false,
            NotificationDeliveryStatus.Failed,
            channel,
            attemptedAt,
            DateTimeOffset.UtcNow,
            "NOTIFICATION_TIMEOUT",
            "The notification provider did not respond in time.",
            correlationId,
            1,
            null);
    }

    public Task<NotificationDeliveryResult> SendAssistanceRequestStatusAsync(
        AssistanceRequestStatusNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Task.FromResult(InvalidInputResult(
                NotificationChannel.Unknown,
                "The assistance request notification is required."));
        }

        var validationError = GetValidationError(request);
        if (validationError is not null)
        {
            return Task.FromResult(InvalidInputResult(request.Channel, validationError));
        }

        var note = string.IsNullOrWhiteSpace(request.Note)
            ? string.Empty
            : $" Note: {request.Note}";

        return SendAsync(
            new NotificationRequest
            {
                Recipient = request.Recipient,
                Channel = request.Channel,
                EventId = request.EventId,
                Subject = $"Assistance request status: {request.Status}",
                Message = $"Assistance request {request.AssistanceRequestId} is now {request.Status}.{note}"
            },
            cancellationToken);
    }

    public Task<NotificationDeliveryResult> SendAllocationAssignmentAsync(
        AllocationAssignmentNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Task.FromResult(InvalidInputResult(
                NotificationChannel.Unknown,
                "The allocation notification is required."));
        }

        var validationError = GetValidationError(request);
        if (validationError is not null)
        {
            return Task.FromResult(InvalidInputResult(request.Channel, validationError));
        }

        var resource = string.IsNullOrWhiteSpace(request.ResourceName)
            ? "resource"
            : request.ResourceName;
        var assignment = string.IsNullOrWhiteSpace(request.AssignmentName)
            ? "an assigned team"
            : request.AssignmentName;

        return SendAsync(
            new NotificationRequest
            {
                Recipient = request.Recipient,
                Channel = request.Channel,
                EventId = request.EventId,
                Subject = "Resource allocation and assignment update",
                Message = $"Allocation {request.AllocationId} for assistance request " +
                          $"{request.AssistanceRequestId} allocated {request.Quantity} {resource} " +
                          $"to {assignment}."
            },
            cancellationToken);
    }

    private static string? GetValidationError(object request)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            errors,
            validateAllProperties: true);

        return errors.FirstOrDefault()?.ErrorMessage;
    }

    private NotificationDeliveryResult InvalidInputResult(
        NotificationChannel channel,
        string? validationError)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var correlationId = Guid.NewGuid();
        _logger.LogWarning(
            "Rejected notification request with {ErrorCode} for {Channel}, correlation {CorrelationId}",
            "INVALID_NOTIFICATION_REQUEST",
            channel,
            correlationId);

        return new NotificationDeliveryResult(
            false,
            NotificationDeliveryStatus.InvalidInput,
            channel,
            timestamp,
            timestamp,
            "INVALID_NOTIFICATION_REQUEST",
            validationError ?? "The notification request is invalid.",
            correlationId,
            0,
            null);
    }
}