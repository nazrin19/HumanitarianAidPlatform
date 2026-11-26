using HumanitarianAidPlatform.Domain.Entities.Notifications;

namespace HumanitarianAidPlatform.Application.Notifications;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken);

    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task UpdateAsync(Notification notification, CancellationToken cancellationToken);
}
