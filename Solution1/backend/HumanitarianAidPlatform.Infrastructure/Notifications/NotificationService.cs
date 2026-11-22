using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Infrastructure.External;
using Microsoft.Extensions.Logging;

namespace HumanitarianAidPlatform.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailSender emailSender,
        ISmsSender smsSender,
        ILogger<NotificationService> logger)
    {
        _emailSender = emailSender;
        _smsSender = smsSender;
        _logger = logger;
    }

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRecipient(request.Recipient);
        if (!Enum.IsDefined(request.Channel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Channel),
                "The requested notification channel is invalid.");
        }

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

            result = result with { CorrelationId = correlationId };

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
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Notification delivery was cancelled through {Channel}, correlation {CorrelationId}",
                request.Channel,
                correlationId);

            return new NotificationDeliveryResult(
                false,
                NotificationDeliveryStatus.Cancelled,
                request.Channel,
                attemptedAt,
                DateTimeOffset.UtcNow,
                "NOTIFICATION_CANCELLED",
                "The notification delivery was cancelled.",
                correlationId,
                1,
                null);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Notification delivery threw an unexpected error through {Channel}, correlation {CorrelationId}",
                request.Channel,
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

    public Task<NotificationDeliveryResult> SendAssistanceRequestStatusAsync(
        AssistanceRequestStatusNotificationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRecipient(request.Recipient);
        if (!Enum.IsDefined(request.Status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Status),
                "The assistance request status is invalid.");
        }
        if (request.AssistanceRequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "AssistanceRequestId must not be empty.",
                nameof(request.AssistanceRequestId));
        }

        var note = string.IsNullOrWhiteSpace(request.Note)
            ? string.Empty
            : $" Note: {request.Note}";

        return SendAsync(
            new NotificationRequest
            {
                Recipient = request.Recipient,
                Channel = request.Channel,
                Subject = $"Assistance request status: {request.Status}",
                Message = $"Assistance request {request.AssistanceRequestId} is now {request.Status}.{note}"
            },
            cancellationToken);
    }

    public Task<NotificationDeliveryResult> SendAllocationAssignmentAsync(
        AllocationAssignmentNotificationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRecipient(request.Recipient);
        if (request.AllocationId == Guid.Empty)
        {
            throw new ArgumentException(
                "AllocationId must not be empty.",
                nameof(request.AllocationId));
        }
        if (request.AssistanceRequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "AssistanceRequestId must not be empty.",
                nameof(request.AssistanceRequestId));
        }
        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity),
                "Quantity must be greater than zero.");
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
                Subject = "Resource allocation and assignment update",
                Message = $"Allocation {request.AllocationId} for assistance request " +
                          $"{request.AssistanceRequestId} allocated {request.Quantity} {resource} " +
                          $"to {assignment}."
            },
            cancellationToken);
    }

    private static void ValidateRecipient(string recipient)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException(
                "Recipient must not be whitespace.",
                nameof(recipient));
        }
    }
}