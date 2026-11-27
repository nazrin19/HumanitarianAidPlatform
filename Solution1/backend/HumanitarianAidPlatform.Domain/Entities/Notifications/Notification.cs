using System.ComponentModel.DataAnnotations.Schema;

namespace HumanitarianAidPlatform.Domain.Entities.Notifications;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public NotificationChannel Channel { get; set; } = NotificationChannel.Unknown;

    [Obsolete("Use Channel. This compatibility property maps to the legacy data-column name.")]
    [NotMapped]
    public string Type
    {
        get => Channel switch
        {
            NotificationChannel.Sms => "SMS",
            NotificationChannel.Email => "Email",
            NotificationChannel.InApp => "InApp",
            _ => "Unknown"
        };
        set
        {
            Channel = Enum.TryParse<NotificationChannel>(value, true, out var channel)
                ? channel
                : NotificationChannel.Unknown;
        }
    }

    public bool IsSent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public Guid? EventId { get; set; }
    public string DeliveryStatus { get; set; } = "Pending";
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CorrelationId { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? DeliveryCompletedAt { get; set; }
}