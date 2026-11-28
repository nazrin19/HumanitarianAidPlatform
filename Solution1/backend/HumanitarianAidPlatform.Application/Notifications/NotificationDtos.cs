using System.ComponentModel.DataAnnotations;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Notifications;

/// <summary>Describes a generic notification to deliver through an application-selected channel.</summary>
public sealed record NotificationRequest : IValidatableObject
{
    /// <summary>
    /// Optional stable event identifier used to suppress recent duplicate
    /// deliveries to the same recipient through the same channel.
    /// </summary>
    public Guid? EventId { get; init; }

    [Required, RegularExpression(@".*\S.*", ErrorMessage = "Recipient must not be blank.")]
    public required string Recipient { get; init; }

    [MaxLength(200)]
    public string? Subject { get; init; }

    [MaxLength(200)]
    public string? Title { get; init; }

    [RegularExpression(@".*\S.*", ErrorMessage = "Message must not be blank.")]
    [MaxLength(4000)]
    public string? Message { get; init; }

    [MaxLength(4000)]
    public string? Body { get; init; }

    public NotificationChannel Channel { get; init; } = NotificationChannel.Email;
    public NotificationEventType EventType { get; init; } = NotificationEventType.General;
    public string Language { get; init; } = "en";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return NotificationRequestValidation.ValidateCommon(
            EventId,
            Channel,
            Recipient,
            EventType,
            Language);
    }
}

/// <summary>Describes an assistance-request status update notification.</summary>
public sealed record AssistanceRequestStatusNotificationRequest : IValidatableObject
{
    /// <summary>Optional stable identifier for this status-change event.</summary>
    public Guid? EventId { get; init; }

    [Required, RegularExpression(@".*\S.*", ErrorMessage = "Recipient must not be blank.")]
    public required string Recipient { get; init; }

    public NotificationChannel Channel { get; init; } = NotificationChannel.Email;

    [MaxLength(200)]
    public string? Title { get; init; }

    [MaxLength(4000)]
    public string? Message { get; init; }

    public NotificationEventType EventType { get; init; } =
        NotificationEventType.ApplicationStatus;

    public string Language { get; init; } = "en";

    [Required]
    public Guid AssistanceRequestId { get; init; }

    public RequestStatus Status { get; init; }

    [MaxLength(1000)]
    public string? Note { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in NotificationRequestValidation.ValidateCommon(
                     EventId,
                     Channel,
                     Recipient,
                     EventType,
                     Language))
        {
            yield return validationResult;
        }

        if (AssistanceRequestId == Guid.Empty)
        {
            yield return new ValidationResult(
                "AssistanceRequestId must not be empty.",
                [nameof(AssistanceRequestId)]);
        }

        if (!Enum.IsDefined(Status))
        {
            yield return new ValidationResult(
                "The assistance request status is invalid.",
                [nameof(Status)]);
        }
    }
}

/// <summary>Describes a resource allocation or assignment notification.</summary>
public sealed record AllocationAssignmentNotificationRequest : IValidatableObject
{
    /// <summary>Optional stable identifier for this allocation event.</summary>
    public Guid? EventId { get; init; }

    [Required, RegularExpression(@".*\S.*", ErrorMessage = "Recipient must not be blank.")]
    public required string Recipient { get; init; }

    public NotificationChannel Channel { get; init; } = NotificationChannel.Email;

    [MaxLength(200)]
    public string? Title { get; init; }

    [MaxLength(4000)]
    public string? Message { get; init; }

    public NotificationEventType EventType { get; init; } =
        NotificationEventType.ShelterAssignment;

    public string Language { get; init; } = "en";

    [Required]
    public Guid AllocationId { get; init; }

    [Required]
    public Guid AssistanceRequestId { get; init; }

    [MaxLength(200)]
    public string? ResourceName { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [MaxLength(200)]
    public string? AssignmentName { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in NotificationRequestValidation.ValidateCommon(
                     EventId,
                     Channel,
                     Recipient,
                     EventType,
                     Language))
        {
            yield return validationResult;
        }

        if (AllocationId == Guid.Empty)
        {
            yield return new ValidationResult(
                "AllocationId must not be empty.",
                [nameof(AllocationId)]);
        }

        if (AssistanceRequestId == Guid.Empty)
        {
            yield return new ValidationResult(
                "AssistanceRequestId must not be empty.",
                [nameof(AssistanceRequestId)]);
        }
    }
}

/// <summary>Represents the outcome and diagnostic details of a notification delivery attempt.</summary>
/// <param name="IsDuplicate">True when this response reuses an earlier delivery outcome.</param>
public sealed record NotificationDeliveryResult(
    bool Succeeded,
    NotificationDeliveryStatus Status,
    NotificationChannel Channel,
    DateTimeOffset AttemptedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorCode,
    string? ErrorMessage,
    Guid? CorrelationId = null,
    int AttemptCount = 1,
    DateTimeOffset? NextRetryAt = null,
    bool IsDuplicate = false);

/// <summary>Represents a persisted notification entry returned to authorized staff.</summary>
public sealed record NotificationHistoryItem(
    Guid Id,
    Guid? EventId,
    string Recipient,
    NotificationChannel Channel,
    DateTimeOffset CreatedAt,
    string Status,
    bool Succeeded,
    DateTimeOffset? CompletedAt,
    Guid? CorrelationId,
    int AttemptCount,
    string? ErrorCode,
    string? ErrorMessage,
    string? Title = null,
    string? Message = null,
    NotificationEventType EventType = NotificationEventType.General,
    string Language = "en",
    bool IsRead = false);

/// <summary>Represents a notification visible to the authenticated recipient.</summary>
public sealed record InAppNotificationItem(
    Guid Id,
    string Title,
    string Message,
    NotificationEventType EventType,
    string Language,
    DateTimeOffset CreatedAt,
    bool IsRead);

internal static class NotificationRequestValidation
{
    public static IEnumerable<ValidationResult> ValidateCommon(
        Guid? eventId,
        NotificationChannel channel,
        string? recipient,
        NotificationEventType eventType,
        string? language)
    {
        if (eventId == Guid.Empty)
        {
            yield return new ValidationResult(
                "EventId must be omitted or a non-empty GUID.",
                ["EventId"]);
        }

        if (!Enum.IsDefined(channel) || channel == NotificationChannel.Unknown)
        {
            yield return new ValidationResult(
                "The requested notification channel is invalid.",
                ["Channel"]);
            yield break;
        }

        if (!Enum.IsDefined(eventType))
        {
            yield return new ValidationResult(
                "The notification event type is invalid.",
                ["EventType"]);
        }

        if (language is not null && language.Trim().Length > 10)
        {
            yield return new ValidationResult(
                "Language code must be 10 characters or fewer.",
                ["Language"]);
        }

        if (string.IsNullOrWhiteSpace(recipient))
        {
            yield return new ValidationResult(
                "Recipient must not be blank.",
                ["Recipient"]);
            yield break;
        }

        var trimmedRecipient = recipient.Trim();
        if (channel == NotificationChannel.Email &&
            !new EmailAddressAttribute().IsValid(trimmedRecipient))
        {
            yield return new ValidationResult(
                "Recipient must be a valid email address for the Email channel.",
                ["Recipient"]);
        }
        else if (channel == NotificationChannel.Sms &&
                 !IsValidPhoneRecipient(trimmedRecipient))
        {
            yield return new ValidationResult(
                "Recipient must be a valid phone number for the Sms channel.",
                ["Recipient"]);
        }
    }

    private static bool IsValidPhoneRecipient(string recipient)
    {
        var digitCount = recipient.Count(character =>
            character is >= '0' and <= '9');
        return digitCount is >= 7 and <= 15 &&
            new PhoneAttribute().IsValid(recipient);
    }
}
