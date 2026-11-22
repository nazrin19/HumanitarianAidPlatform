using HumanitarianAidPlatform.Api.Controllers;
using HumanitarianAidPlatform.Application.Geocoding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Xunit;

namespace HumanitarianAidPlatform.Tests;

public sealed class GeocodingControllerTests
{
    [Theory]
    [InlineData(GeocodingStatus.Found, 200)]
    [InlineData(GeocodingStatus.Ambiguous, 200)]
    [InlineData(GeocodingStatus.InvalidInput, 400)]
    [InlineData(GeocodingStatus.NotFound, 404)]
    [InlineData(GeocodingStatus.Cancelled, 499)]
    [InlineData(GeocodingStatus.ProviderUnavailable, 503)]
    public async Task ForwardMapsServiceOutcomeToExpectedHttpStatus(
        GeocodingStatus status,
        int expectedStatus)
    {
        var result = CreateResult(status);
        var service = new StubGeocodingService(result);
        var controller = new GeocodingController(service);
        using var cancellation = new CancellationTokenSource();

        var response = await controller.Forward(
            new ForwardGeocodingRequest { Query = "Colombo, Sri Lanka" },
            cancellation.Token);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(response.Result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        Assert.Same(result, objectResult.Value);
        Assert.Equal(cancellation.Token, service.LastCancellationToken);
    }

    [Fact]
    public async Task ReversePreservesCandidateDetailsForAmbiguousResult()
    {
        var result = CreateResult(GeocodingStatus.Ambiguous);
        var controller = new GeocodingController(new StubGeocodingService(result));

        var response = await controller.Reverse(
            new ReverseGeocodingRequest { Latitude = 6.9, Longitude = 79.8 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<GeocodingResult>(ok.Value);
        Assert.True(body.IsAmbiguous);
        Assert.Equal(2, body.Candidates.Count);
        Assert.Equal("Place B", body.Candidates[1].DisplayAddress);
    }

    [Theory]
    [InlineData(nameof(GeocodingController.Forward))]
    [InlineData(nameof(GeocodingController.Reverse))]
    public void GeocodingActionsDocumentValidationAndAuthorizationStatuses(string actionName)
    {
        var action = typeof(GeocodingController).GetMethod(actionName);
        Assert.NotNull(action);
        var statuses = action!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(attribute => attribute.StatusCode)
            .ToArray();

        Assert.Contains(400, statuses);
        Assert.Contains(401, statuses);
        Assert.Contains(403, statuses);
        Assert.NotNull(typeof(GeocodingController).GetCustomAttribute<AuthorizeAttribute>());
    }

    private static GeocodingResult CreateResult(GeocodingStatus status) =>
        new(
            status,
            null,
            null,
            null,
            [
                new GeocodingCandidate(6.9, 79.8, "Place A"),
                new GeocodingCandidate(7.1, 80.1, "Place B")
            ],
            status == GeocodingStatus.Ambiguous,
            status == GeocodingStatus.Found ? null : "GEOCODING_TEST",
            null);

    private sealed class StubGeocodingService(GeocodingResult result)
        : IGeocodingService
    {
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<GeocodingResult> ForwardAsync(
            ForwardGeocodingRequest request,
            CancellationToken cancellationToken)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(result);
        }

        public Task<GeocodingResult> ReverseAsync(
            ReverseGeocodingRequest request,
            CancellationToken cancellationToken)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(result);
        }
    }
}
