using System.Security.Claims;
using HumanitarianAidPlatform.Api.Controllers;
using HumanitarianAidPlatform.Application.Geocoding;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Infrastructure.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HumanitarianAidPlatform.Tests;

public sealed class MappingAndInboxTests
{
    [Fact]
    public void HaversineDistanceCalculatesKnownDistanceBetweenColomboAndKandy()
    {
        // Colombo (6.9271, 79.8612) to Kandy (7.2906, 80.6337) is ~94 km straight line.
        var km = MappingService.CalculateDistanceKm(6.9271, 79.8612, 7.2906, 80.6337);
        Assert.InRange(km, 90, 100);
    }

    [Fact]
    public async Task MappingControllerReturnsRouteAndServiceAreas()
    {
        var expectedRoute = new MapRouteResponse(94.3, "{\"type\":\"LineString\"}", true, "Straight-line estimate.");
        var expectedAreas = new[]
        {
            new ServiceAreaResponse(Guid.NewGuid(), "Western Zone", "{\"type\":\"Polygon\",\"coordinates\":[]}", true, DateTimeOffset.UtcNow)
        };
        var service = new StubMappingService(expectedRoute, expectedAreas);
        var controller = new MappingController(service);

        var routeResult = await controller.GetRoute(
            new MapRouteRequest
            {
                Origin = new MapPoint { Latitude = 6.9271, Longitude = 79.8612 },
                Destination = new MapPoint { Latitude = 7.2906, Longitude = 80.6337 }
            },
            CancellationToken.None);
        var areasResult = await controller.GetServiceAreas(CancellationToken.None);

        var routeOk = Assert.IsType<OkObjectResult>(routeResult.Result);
        Assert.Same(expectedRoute, routeOk.Value);
        var areasOk = Assert.IsType<OkObjectResult>(areasResult.Result);
        Assert.Same(expectedAreas, areasOk.Value);
    }

    [Fact]
    public async Task NotificationInboxControllerScopesRequestsToAuthenticatedEmail()
    {
        var item = new InAppNotificationItem(
            Guid.NewGuid(),
            "Emergency announcement",
            "Evacuate low-lying areas.",
            NotificationEventType.EmergencyAnnouncement,
            "en",
            DateTimeOffset.UtcNow,
            false);
        var service = new StubInboxNotificationService([item]);
        var controller = new NotificationInboxController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim("email", "citizen@example.test")],
                        "TestAuth"))
                }
            }
        };

        var inbox = await controller.GetInbox(30, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(inbox.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<InAppNotificationItem>>(ok.Value);
        Assert.Single(items);
        Assert.Equal("citizen@example.test", service.LastRecipient);

        var markOne = await controller.MarkAsRead(item.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(markOne);

        var markAll = await controller.MarkAllAsRead(CancellationToken.None);
        var markAllOk = Assert.IsType<OkObjectResult>(markAll.Result);
        var response = Assert.IsType<MarkNotificationsReadResponse>(markAllOk.Value);
        Assert.Equal(1, response.UpdatedCount);
    }

    private sealed class StubMappingService(
        MapRouteResponse route,
        IReadOnlyList<ServiceAreaResponse> areas) : IMappingService
    {
        public Task<MapRouteResponse> GetRouteAsync(MapRouteRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(route);

        public Task<IReadOnlyList<MapReferenceResponse>> GetReferencesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MapReferenceResponse>>([]);

        public Task<MapReferenceResponse?> GetReferenceAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<MapReferenceResponse?>(null);

        public Task<MapReferenceResponse> CreateReferenceAsync(CreateMapReferenceRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ServiceAreaResponse>> GetServiceAreasAsync(CancellationToken cancellationToken) =>
            Task.FromResult(areas);

        public Task<ServiceAreaResponse?> GetServiceAreaAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ServiceAreaResponse?>(null);

        public Task<ServiceAreaResponse> CreateServiceAreaAsync(CreateServiceAreaRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubInboxNotificationService(
        IReadOnlyList<InAppNotificationItem> items) : INotificationService
    {
        public string? LastRecipient { get; private set; }

        public Task<IReadOnlyList<NotificationHistoryItem>> GetRecentAsync(int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NotificationHistoryItem>>([]);

        public Task<IReadOnlyList<InAppNotificationItem>> GetInboxAsync(
            string recipient,
            int limit,
            CancellationToken cancellationToken)
        {
            LastRecipient = recipient;
            return Task.FromResult(items);
        }

        public Task<int> MarkAllAsReadAsync(string recipient, CancellationToken cancellationToken)
        {
            LastRecipient = recipient;
            return Task.FromResult(items.Count);
        }

        public Task<bool> MarkAsReadAsync(Guid notificationId, string recipient, CancellationToken cancellationToken)
        {
            LastRecipient = recipient;
            return Task.FromResult(items.Any(item => item.Id == notificationId));
        }

        public Task<NotificationDeliveryResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<NotificationDeliveryResult> SendAssistanceRequestStatusAsync(AssistanceRequestStatusNotificationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<NotificationDeliveryResult> SendAllocationAssignmentAsync(AllocationAssignmentNotificationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
