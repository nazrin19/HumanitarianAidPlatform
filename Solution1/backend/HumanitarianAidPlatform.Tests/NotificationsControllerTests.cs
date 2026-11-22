using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HumanitarianAidPlatform.Tests;

public class NotificationsControllerTests
{
    [Fact]
    public async Task CancellationResultReturns499()
    {
        var controller = new NotificationsController(
            new CancelledNotificationService());

        var response = await controller.Send(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(499, objectResult.StatusCode);

        var result = Assert.IsType<NotificationDeliveryResult>(
            objectResult.Value);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, result.Status);
        Assert.NotNull(result.CorrelationId);
    }

    [Fact]
    public async Task MockResultReturns202Accepted()
    {
        var controller = new NotificationsController(
            new MockNotificationService());

        var response = await controller.Send(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(response.Result);
        Assert.Equal(202, accepted.StatusCode);
    }

    private sealed class CancelledNotificationService : INotificationService
    {
        public Task<NotificationDeliveryResult> SendAsync(
            NotificationRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new NotificationDeliveryResult(
                false,
                NotificationDeliveryStatus.Cancelled,
                NotificationChannel.Email,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                "NOTIFICATION_CANCELLED",
                "The notification delivery was cancelled.",
                Guid.NewGuid(),
                1,
                null));
        }

        public Task<NotificationDeliveryResult> SendAssistanceRequestStatusAsync(
            AssistanceRequestStatusNotificationRequest request,
            CancellationToken cancellationToken) =>
            SendAsync(
                new NotificationRequest
                {
                    Recipient = request.Recipient,
                    Channel = request.Channel,
                    Message = "Status update"
                },
                cancellationToken);

        public Task<NotificationDeliveryResult> SendAllocationAssignmentAsync(
            AllocationAssignmentNotificationRequest request,
            CancellationToken cancellationToken) =>
            SendAsync(
                new NotificationRequest
                {
                    Recipient = request.Recipient,
                    Channel = request.Channel,
                    Message = "Allocation update"
                },
                cancellationToken);
    }

    private sealed class MockNotificationService : INotificationService
    {
        public Task<NotificationDeliveryResult> SendAsync(
            NotificationRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new NotificationDeliveryResult(
                true,
                NotificationDeliveryStatus.MockSent,
                request.Channel,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                "Sandbox mode.",
                Guid.NewGuid(),
                1,
                null));
        }

        public Task<NotificationDeliveryResult> SendAssistanceRequestStatusAsync(
            AssistanceRequestStatusNotificationRequest request,
            CancellationToken cancellationToken) =>
            SendAsync(
                new NotificationRequest
                {
                    Recipient = request.Recipient,
                    Channel = request.Channel,
                    Message = "Status update"
                },
                cancellationToken);

        public Task<NotificationDeliveryResult> SendAllocationAssignmentAsync(
            AllocationAssignmentNotificationRequest request,
            CancellationToken cancellationToken) =>
            SendAsync(
                new NotificationRequest
                {
                    Recipient = request.Recipient,
                    Channel = request.Channel,
                    Message = "Allocation update"
                },
                cancellationToken);
    }
}
