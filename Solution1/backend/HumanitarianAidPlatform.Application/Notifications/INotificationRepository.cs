using HumanitarianAidPlatform.Domain.Entities.Notifications;

namespace HumanitarianAidPlatform.Application.Notifications;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Notification>> GetInboxAsync(
        string recipient,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Notification>>([]);

    Task<int> MarkAllAsReadAsync(
        string recipient,
        CancellationToken cancellationToken) =>
        Task.FromResult(0);

    Task<bool> MarkAsReadAsync(
        Guid notificationId,
        string recipient,
        CancellationToken cancellationToken) =>
        Task.FromResult(false);

    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task UpdateAsync(Notification notification, CancellationToken cancellationToken);
}
