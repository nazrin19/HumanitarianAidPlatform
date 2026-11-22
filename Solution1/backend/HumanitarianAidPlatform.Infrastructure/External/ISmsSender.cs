using HumanitarianAidPlatform.Application.Notifications;

namespace HumanitarianAidPlatform.Infrastructure.External;

public interface ISmsSender
{
    Task<NotificationDeliveryResult> SendAsync(
        string recipient,
        string message,
        Guid correlationId,
        CancellationToken cancellationToken);
}
