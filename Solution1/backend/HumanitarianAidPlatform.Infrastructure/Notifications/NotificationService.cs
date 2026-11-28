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
    private readonly INotificationRepository? _notificationRepository;
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
            null,
            logger)
    {
    }

    public NotificationService(
        IEmailSender emailSender,
        ISmsSender smsSender,
        NotificationDeliveryDeduplicator deduplicator,
        ILogger<NotificationService> logger)
        : this(
            emailSender,
            smsSender,
            deduplicator,
            null,
            logger)
    {
    }

    public NotificationService(
        IEmailSender emailSender,
        ISmsSender smsSender,
        NotificationDeliveryDeduplicator deduplicator,
        INotificationRepository? notificationRepository,
        ILogger<NotificationService> logger)
    {
        _emailSender = emailSender;
        _smsSender = smsSender;
        _deduplicator = deduplicator;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationHistoryItem>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        if (_notificationRepository is null)
        {
            throw new NotificationPersistenceException(
                "ReadHistory",
                "Notification history is unavailable because no repository is configured.");
        }

        var notifications = await _notificationRepository.GetRecentAsync(
            Math.Clamp(limit, 1, 100),
            cancellationToken);

        return notifications
            .Select(notification => new NotificationHistoryItem(
                notification.Id,
                notification.EventId,
                RecipientMasking.Mask(notification.Recipient),
                notification.Channel,
                ToUtcOffset(notification.CreatedAt),
                notification.DeliveryStatus,
                notification.IsSent,
                notification.DeliveryCompletedAt is DateTime completedAt
                    ? ToUtcOffset(completedAt)
                    : null,
                notification.CorrelationId,
                notification.AttemptCount,
                notification.ErrorCode,
                notification.ErrorMessage,
                notification.Title,
                notification.Message,
                notification.EventType,
                notification.Language,
                notification.IsRead))
            .ToArray();
    }

    public async Task<IReadOnlyList<InAppNotificationItem>> GetInboxAsync(
        string recipient,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("Recipient is required.", nameof(recipient));
        }

        if (_notificationRepository is null)
        {
            throw new NotificationPersistenceException(
                "ReadInbox",
                "The notification inbox is unavailable because no repository is configured.");
        }

        var notifications = await _notificationRepository.GetInboxAsync(
            recipient,
            Math.Clamp(limit, 1, 100),
            cancellationToken);

        return notifications
            .Select(notification => new InAppNotificationItem(
                notification.Id,
                notification.Title,
                notification.Message,
                notification.EventType,
                notification.Language,
                ToUtcOffset(notification.CreatedAt),
                notification.IsRead))
            .ToArray();
    }

    public async Task<int> MarkAllAsReadAsync(
        string recipient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("Recipient is required.", nameof(recipient));
        }

        if (_notificationRepository is null)
        {
            throw new NotificationPersistenceException(
                "MarkInboxRead",
                "The notification inbox is unavailable because no repository is configured.");
        }

        return await _notificationRepository.MarkAllAsReadAsync(
            recipient,
            cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(
        Guid notificationId,
        string recipient,
        CancellationToken cancellationToken)
    {
        if (notificationId == Guid.Empty || string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("A notification ID and recipient are required.");
        }

        if (_notificationRepository is null)
        {
            throw new NotificationPersistenceException(
                "MarkInboxItemRead",
                "The notification inbox is unavailable because no repository is configured.");
        }

        return await _notificationRepository.MarkAsReadAsync(
            notificationId,
            recipient,
            cancellationToken);
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
                    "Duplicate notification event {EventId} reused prior delivery outcome {DeliveryStatus} (succeeded: {Succeeded}) for {Channel}, correlation {CorrelationId}",
                    eventId,
                    result.Status,
                    result.Succeeded,
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
        var language = NotificationMessageBuilder.NormalizeLanguage(request.Language);
        var generatedContent = request.EventType switch
        {
            NotificationEventType.EmergencyAnnouncement =>
                NotificationMessageBuilder.Emergency(request.Message ?? request.Body, language),
            _ => NotificationMessageBuilder.General(language)
        };
        var title = FirstNonBlank(request.Title, request.Subject, generatedContent.Title);
        var message = FirstNonBlank(request.Message, request.Body, generatedContent.Message);
        var notification = new Notification
        {
            Recipient = request.Recipient,
            Title = title,
            Subject = title,
            Message = message,
            Channel = request.Channel,
            EventType = request.EventType,
            Language = language,
            EventId = request.EventId,
            CorrelationId = correlationId,
            AttemptCount = 1,
            CreatedAt = attemptedAt.UtcDateTime,
            DeliveryStatus = "Pending"
        };

        if (_notificationRepository is not null)
        {
            try
            {
                await _notificationRepository.AddAsync(
                    notification,
                    CancellationToken.None);
            }
            catch (NotificationPersistenceException exception)
            {
                _logger.LogError(
                    exception,
                    "Notification delivery was not attempted because its pending record could not be persisted for {Channel}, correlation {CorrelationId}.",
                    request.Channel,
                    correlationId);
                return PersistenceFailureResult(
                    request.Channel,
                    correlationId,
                    attemptedAt,
                    "The notification was not sent because its pending attempt could not be persisted.");
            }
        }
        else
        {
            _logger.LogWarning(
                "Notification history is not configured; attempting non-persisted delivery through {Channel}, correlation {CorrelationId}.",
                request.Channel,
                correlationId);
        }

        _logger.LogInformation(
            "Routing notification through {Channel} for recipient {RecipientMask}, correlation {CorrelationId}",
            request.Channel,
            recipientMask,
            correlationId);

        NotificationDeliveryResult result;
        try
        {
            result = request.Channel switch
            {
                NotificationChannel.Email => await _emailSender.SendAsync(
                    request.Recipient,
                    title,
                    message,
                    correlationId,
                    cancellationToken),
                NotificationChannel.Sms => await _smsSender.SendAsync(
                    request.Recipient,
                    message,
                    correlationId,
                    cancellationToken),
                NotificationChannel.InApp => CreateInAppSimulationResult(
                    attemptedAt,
                    correlationId,
                    recipientMask),
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
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Notification delivery was cancelled through {Channel}, correlation {CorrelationId}",
                request.Channel,
                correlationId);

            result = CancelledResult(request.Channel, correlationId, attemptedAt);
        }
        catch (TimeoutException)
        {
            result = ProviderTimeoutResult(request.Channel, correlationId, attemptedAt);
        }
        catch (OperationCanceledException)
        {
            result = ProviderTimeoutResult(request.Channel, correlationId, attemptedAt);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Notification delivery failed through {Channel} with {ErrorCode} ({ExceptionType}), correlation {CorrelationId}",
                request.Channel,
                "NOTIFICATION_DELIVERY_ERROR",
                exception.GetType().Name,
                correlationId);

            result = new NotificationDeliveryResult(
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

        result = result with
        {
            CorrelationId = correlationId,
            AttemptCount = Math.Max(result.AttemptCount, 1)
        };

        if (_notificationRepository is not null)
        {
            var wasSent = result.Succeeded &&
                result.Status is NotificationDeliveryStatus.Sent or
                    NotificationDeliveryStatus.MockSent;
            notification.IsSent = wasSent;
            notification.SentAt = wasSent
                ? (result.CompletedAt ?? DateTimeOffset.UtcNow).UtcDateTime
                : null;
            notification.DeliveryStatus = result.Status.ToString();
            notification.ErrorCode = result.ErrorCode;
            notification.ErrorMessage = result.ErrorMessage;
            notification.CorrelationId = result.CorrelationId;
            notification.AttemptCount = result.AttemptCount;
            notification.DeliveryCompletedAt = result.CompletedAt?.UtcDateTime;
            try
            {
                await _notificationRepository.UpdateAsync(
                    notification,
                    CancellationToken.None);
            }
            catch (NotificationPersistenceException exception)
            {
                _logger.LogError(
                    exception,
                    "Notification result could not be persisted after provider completion through {Channel}; provider succeeded {Succeeded}, status {DeliveryStatus}, correlation {CorrelationId}.",
                    request.Channel,
                    result.Succeeded,
                    result.Status,
                    correlationId);

                if (result.Succeeded)
                {
                    result = result with
                    {
                        ErrorCode = "NOTIFICATION_STATUS_PERSISTENCE_FAILED",
                        ErrorMessage = "Delivery succeeded, but its final status could not be persisted."
                    };
                }
            }
        }

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

    private NotificationDeliveryResult CreateInAppSimulationResult(
        DateTimeOffset attemptedAt,
        Guid correlationId,
        string recipientMask)
    {
        _logger.LogWarning(
            "In-app notification for recipient {RecipientMask} is simulation-only; no user-facing inbox delivery occurred, correlation {CorrelationId}",
            recipientMask,
            correlationId);

        return new NotificationDeliveryResult(
            true,
            NotificationDeliveryStatus.MockSent,
            NotificationChannel.InApp,
            attemptedAt,
            DateTimeOffset.UtcNow,
            null,
            "In-app delivery is a simulation only; no user-facing inbox delivery occurred.",
            correlationId,
            1,
            null);
    }

    private static NotificationDeliveryResult PersistenceFailureResult(
        NotificationChannel channel,
        Guid correlationId,
        DateTimeOffset attemptedAt,
        string message) =>
        new(
            false,
            NotificationDeliveryStatus.Failed,
            channel,
            attemptedAt,
            DateTimeOffset.UtcNow,
            "NOTIFICATION_PERSISTENCE_FAILED",
            message,
            correlationId,
            0,
            null);

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

        var template = NotificationMessageBuilder.ApplicationStatus(
            request.AssistanceRequestId,
            request.Status,
            request.Note,
            request.Language);

        return SendAsync(
            new NotificationRequest
            {
                Recipient = request.Recipient,
                Channel = request.Channel,
                EventId = request.EventId,
                Subject = FirstNonBlank(request.Title, template.Title),
                Title = FirstNonBlank(request.Title, template.Title),
                Message = FirstNonBlank(request.Message, template.Message),
                EventType = request.EventType,
                Language = request.Language
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

        var template = NotificationMessageBuilder.Allocation(
            request.AssistanceRequestId,
            request.Quantity,
            request.ResourceName,
            request.AssignmentName,
            request.Language);

        return SendAsync(
            new NotificationRequest
            {
                Recipient = request.Recipient,
                Channel = request.Channel,
                EventId = request.EventId,
                Subject = FirstNonBlank(request.Title, template.Title),
                Title = FirstNonBlank(request.Title, template.Title),
                Message = FirstNonBlank(request.Message, template.Message),
                EventType = request.EventType,
                Language = request.Language
            },
            cancellationToken);
    }

    private static string FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? string.Empty;

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

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

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