using System.ComponentModel.DataAnnotations;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Notifications;

/// <summary>Describes a generic notification to deliver through an application-selected channel.</summary>
public sealed record NotificationRequest
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

    [Required]
    [MaxLength(4000)]
    public required string Message { get; init; }

    public NotificationChannel Channel { get; init; } = NotificationChannel.Email;
}

/// <summary>Describes an assistance-request status update notification.</summary>
public sealed record AssistanceRequestStatusNotificationRequest
{
    /// <summary>Optional stable identifier for this status-change event.</summary>
    public Guid? EventId { get; init; }

    [Required, RegularExpression(@".*\S.*", ErrorMessage = "Recipient must not be blank.")]
    public required string Recipient { get; init; }

    public NotificationChannel Channel { get; init; } = NotificationChannel.Email;

    [Required]
    public Guid AssistanceRequestId { get; init; }

    public RequestStatus Status { get; init; }

    [MaxLength(1000)]
    public string? Note { get; init; }
}

/// <summary>Describes a resource allocation or assignment notification.</summary>
public sealed record AllocationAssignmentNotificationRequest
{
    /// <summary>Optional stable identifier for this allocation event.</summary>
    public Guid? EventId { get; init; }

    [Required, RegularExpression(@".*\S.*", ErrorMessage = "Recipient must not be blank.")]
    public required string Recipient { get; init; }

    public NotificationChannel Channel { get; init; } = NotificationChannel.Email;

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
