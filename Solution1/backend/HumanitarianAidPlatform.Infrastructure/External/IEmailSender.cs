using HumanitarianAidPlatform.Application.Notifications;

namespace HumanitarianAidPlatform.Infrastructure.External;

public interface IEmailSender
{
    Task<NotificationDeliveryResult> SendAsync(
        string recipient,
        string? subject,
        string message,
        Guid correlationId,
        CancellationToken cancellationToken);
}
