using HumanitarianAidPlatform.Domain.Entities.Notifications;

namespace HumanitarianAidPlatform.Application.Notifications;

/// <summary>Provides channel-neutral delivery operations for application notifications.</summary>
public interface INotificationService
{
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