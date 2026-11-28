namespace HumanitarianAidPlatform.Application.Notifications;

/// <summary>Provides channel-neutral delivery operations for application notifications.</summary>
public interface INotificationService
{
    Task<IReadOnlyList<NotificationHistoryItem>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InAppNotificationItem>> GetInboxAsync(
        string recipient,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<InAppNotificationItem>>([]);

    Task<int> MarkAllAsReadAsync(
        string recipient,
        CancellationToken cancellationToken) =>
        Task.FromResult(0);

    Task<bool> MarkAsReadAsync(
        Guid notificationId,
        string recipient,
        CancellationToken cancellationToken) =>
        Task.FromResult(false);

    Task<NotificationDeliveryResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken);

    Task<NotificationDeliveryResult> SendAssistanceRequestStatusAsync(
        AssistanceRequestStatusNotificationRequest request,
        CancellationToken cancellationToken);

    Task<NotificationDeliveryResult> SendAllocationAssignmentAsync(
        AllocationAssignmentNotificationRequest request,
        CancellationToken cancellationToken);
}