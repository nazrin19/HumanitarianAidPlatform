using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Api.Controllers;
using HumanitarianAidPlatform.Infrastructure.External;
using HumanitarianAidPlatform.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
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

    [Fact]
    public async Task InvalidInputResultReturns400()
    {
        var controller = new NotificationsController(new InvalidInputNotificationService());

        var response = await controller.Send(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Unknown
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(
            NotificationDeliveryStatus.InvalidInput,
            Assert.IsType<NotificationDeliveryResult>(badRequest.Value).Status);
    }

    [Theory]
    [InlineData(NotificationChannel.Unknown)]
    [InlineData((NotificationChannel)999)]
    public async Task InvalidChannelIsRejectedAs400BeforeDelivery(NotificationChannel channel)
    {
        var controller = CreateValidatingController();

        var response = await controller.Send(
            new NotificationRequest
            {
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = channel
            },
            CancellationToken.None);

        AssertInvalidInputBadRequest(response);
    }

    [Fact]
    public async Task EmptyEventIdIsRejectedAs400()
    {
        var controller = CreateValidatingController();

        var response = await controller.Send(
            new NotificationRequest
            {
                EventId = Guid.Empty,
                Recipient = "citizen@example.test",
                Message = "Approved",
                Channel = NotificationChannel.Email
            },
            CancellationToken.None);

        AssertInvalidInputBadRequest(response);
    }

    [Fact]
    public async Task EmptyAllocationIdIsRejectedAs400()
    {
        var controller = CreateValidatingController();

        var response = await controller.SendAllocationAssignment(
            new AllocationAssignmentNotificationRequest
            {
                Recipient = "citizen@example.test",
                AllocationId = Guid.Empty,
                AssistanceRequestId = Guid.NewGuid(),
                Quantity = 1
            },
            CancellationToken.None);

        AssertInvalidInputBadRequest(response);
    }

    [Theory]
    [InlineData(nameof(NotificationsController.Send))]
    [InlineData(nameof(NotificationsController.SendAssistanceRequestStatus))]
    [InlineData(nameof(NotificationsController.SendAllocationAssignment))]
    public void NotificationActionsDocumentValidationAndAuthorizationStatuses(string actionName)
    {
        var action = typeof(NotificationsController).GetMethod(actionName);
        Assert.NotNull(action);
        var statuses = action!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(attribute => attribute.StatusCode)
            .ToArray();

        Assert.Contains(400, statuses);
        Assert.Contains(401, statuses);
        Assert.Contains(403, statuses);
        Assert.NotNull(typeof(NotificationsController).GetCustomAttribute<AuthorizeAttribute>());
    }

    private sealed class InvalidInputNotificationService : INotificationService
    {
        private static NotificationDeliveryResult InvalidResult(NotificationChannel channel) =>
            new(
                false,
                NotificationDeliveryStatus.InvalidInput,
                channel,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                "INVALID_NOTIFICATION_REQUEST",
                "The notification request is invalid.",
                Guid.NewGuid(),
                0,
                null);

        public Task<NotificationDeliveryResult> SendAsync(
            NotificationRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(InvalidResult(request.Channel));

        public Task<NotificationDeliveryResult> SendAssistanceRequestStatusAsync(
            AssistanceRequestStatusNotificationRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(InvalidResult(request.Channel));

        public Task<NotificationDeliveryResult> SendAllocationAssignmentAsync(
            AllocationAssignmentNotificationRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(InvalidResult(request.Channel));
    }

    private static NotificationsController CreateValidatingController() =>
        new(new NotificationService(
            new NeverEmailSender(),
            new NeverSmsSender(),
            NullLogger<NotificationService>.Instance));

    private static void AssertInvalidInputBadRequest(
        ActionResult<NotificationDeliveryResult> response)
    {
        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(
            NotificationDeliveryStatus.InvalidInput,
            Assert.IsType<NotificationDeliveryResult>(badRequest.Value).Status);
    }

    private sealed class NeverEmailSender : IEmailSender
    {
        public Task<NotificationDeliveryResult> SendAsync(
            string recipient,
            string? subject,
            string message,
            Guid correlationId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Invalid notification input reached the sender.");
    }

    private sealed class NeverSmsSender : ISmsSender
    {
        public Task<NotificationDeliveryResult> SendAsync(
            string recipient,
            string message,
            Guid correlationId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Invalid notification input reached the sender.");
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
