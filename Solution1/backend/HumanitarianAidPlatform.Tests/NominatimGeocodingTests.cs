using System.Net;
using System.Text;
using HumanitarianAidPlatform.Application.Geocoding;
using HumanitarianAidPlatform.Infrastructure.External;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace HumanitarianAidPlatform.Tests;

public class NominatimGeocodingTests
{
    [Fact]
    public async Task ForwardLookupReturnsValidatedCoordinatesAndAddress()
    {
        var (service, client) = CreateClient((_, _) =>
            Task.FromResult(JsonResponse(
                """[{"lat":"6.9271","lon":"79.8612","display_name":"Colombo, Sri Lanka"}]""")));
        using (client)

        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = " Colombo, Sri Lanka " },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.Found, result.Status);
            Assert.Equal(6.9271, result.Latitude);
            Assert.Equal(79.8612, result.Longitude);
            Assert.Equal("Colombo, Sri Lanka", result.DisplayAddress);
            Assert.Single(result.Candidates);
        }
    }

    [Fact]
    public async Task ForwardLookupEncodesUserQueryInRequestUri()
    {
        Uri? requestUri = null;
        var (service, client) = CreateClient((request, _) =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(JsonResponse("[]"));
        });
        using (client)
        {
            await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo, Sri Lanka" },
                CancellationToken.None);

            Assert.NotNull(requestUri);
            Assert.Contains("q=Colombo%2C%20Sri%20Lanka", requestUri!.Query);
        }
    }

    [Fact]
    public async Task ReverseLookupReturnsFormattedAddressAndCoordinates()
    {
        var (service, client) = CreateClient((_, _) =>
            Task.FromResult(JsonResponse(
                """{"lat":"6.9271","lon":"79.8612","display_name":"Colombo, Sri Lanka"}""")));
        using (client)
        {
            var result = await service.ReverseAsync(
                new ReverseGeocodingRequest
                {
                    Latitude = 6.9271,
                    Longitude = 79.8612
                },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.Found, result.Status);
            Assert.Equal("Colombo, Sri Lanka", result.DisplayAddress);
            Assert.Equal(6.9271, result.Latitude);
            Assert.Equal(79.8612, result.Longitude);
        }
    }

    [Fact]
    public async Task ReverseLookupMapsProviderNoResultToNotFound()
    {
        var (service, client) = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""{"error":"Unable to geocode"}""")));
        using (client)
        {
            var result = await service.ReverseAsync(
                new ReverseGeocodingRequest
                {
                    Latitude = 0,
                    Longitude = 0
                },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.NotFound, result.Status);
            Assert.Null(result.Latitude);
            Assert.Empty(result.Candidates);
        }
    }

    [Fact]
    public async Task EmptyForwardResultsReturnNotFound()
    {
        var callCount = 0;
        var (service, client) = CreateClient((_, _) =>
        {
            callCount++;
            return Task.FromResult(JsonResponse("[]"));
        });
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Nowhere, Imaginary" },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.NotFound, result.Status);
            Assert.Empty(result.Candidates);
            Assert.False(result.IsAmbiguous);
            Assert.Equal(1, callCount);
        }
    }

    [Fact]
    public async Task MultipleForwardResultsAreReturnedAsAmbiguousWithoutSelectingOne()
    {
        var callCount = 0;
        var (service, client) = CreateClient((_, _) =>
        {
            callCount++;
            return Task.FromResult(JsonResponse(
                """[{"lat":"6.9","lon":"79.8","display_name":"Place A"},{"lat":"7.1","lon":"80.1","display_name":"Place B"}]"""));
        });
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Springfield, Example" },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.Ambiguous, result.Status);
            Assert.True(result.IsAmbiguous);
            Assert.Null(result.Latitude);
            Assert.Null(result.Longitude);
            Assert.Equal(2, result.Candidates.Count);
            Assert.Equal(1, callCount);
        }
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[null]")]
    [InlineData("""[{"lat":"not-a-number","lon":"79.8","display_name":"Bad"}]""")]
    [InlineData("""[{"lat":"91","lon":"79.8","display_name":"Out of range"}]""")]
    [InlineData("""[{"lat":"6.9","lon":"79.8"}]""")]
    public async Task MalformedJsonOrCoordinatesReturnSafeProviderFailure(string json)
    {
        var (service, client) = CreateClient((_, _) =>
            Task.FromResult(JsonResponse(json)));
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest
                {
                    Query = "Private address text"
                },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.ProviderUnavailable, result.Status);
            Assert.Equal("GEOCODING_INVALID_RESPONSE", result.ErrorCode);
            Assert.DoesNotContain("Private address text", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task HttpFailureReturnsProviderUnavailableWithoutResponseBodyLeak()
    {
        var logger = new RecordingLogger();
        var (service, client) = CreateClient(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("internal provider error with secret")
            }),
            logger,
            maximumRetries: 0);
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.ProviderUnavailable, result.Status);
            Assert.Equal("GEOCODING_HTTP_ERROR", result.ErrorCode);
            Assert.DoesNotContain(
                "secret",
                string.Join(" ", logger.Messages),
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task NetworkFailureReturnsSafeProviderUnavailableResult()
    {
        var callCount = 0;
        var (service, client) = CreateClient((_, _) =>
        {
            callCount++;
            throw new HttpRequestException("private host details");
        });
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.ProviderUnavailable, result.Status);
            Assert.Equal("GEOCODING_NETWORK_ERROR", result.ErrorCode);
            Assert.DoesNotContain("private host details", result.ErrorMessage);
            Assert.Equal(3, callCount);
        }
    }

    [Fact]
    public async Task HttpClientTimeoutRetriesAreBounded()
    {
        var callCount = 0;
        var (service, client) = CreateClient(
            async (_, cancellationToken) =>
            {
                callCount++;
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
            timeout: TimeSpan.FromMilliseconds(50),
            maximumRetries: 2);
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.ProviderUnavailable, result.Status);
            Assert.Equal("GEOCODING_TIMEOUT", result.ErrorCode);
            Assert.Equal(3, callCount);
        }
    }

    [Fact]
    public async Task ConfiguredNominatimTimeoutCancelsInFlightRequest()
    {
        var observedCancellation = false;
        var (service, client) = CreateClient(
            async (_, cancellationToken) =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return JsonResponse("[]");
                }
                catch (OperationCanceledException)
                {
                    observedCancellation = true;
                    throw;
                }
            },
            timeoutSeconds: 1,
            maximumRetries: 0);
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.ProviderUnavailable, result.Status);
            Assert.Equal("GEOCODING_TIMEOUT", result.ErrorCode);
            Assert.True(observedCancellation);
        }
    }

    [Fact]
    public async Task CallerCancellationDuringProviderRequestIsNotRetried()
    {
        var started = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        var (service, client) = CreateClient(
            async (_, cancellationToken) =>
            {
                callCount++;
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return JsonResponse("[]");
            });
        using (client)
        using (var cancellation = new CancellationTokenSource())
        {
            var lookup = service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                cancellation.Token);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            cancellation.Cancel();

            var result = await lookup;

            Assert.Equal(GeocodingStatus.Cancelled, result.Status);
            Assert.Equal("GEOCODING_CANCELLED", result.ErrorCode);
            Assert.Equal(1, callCount);
        }
    }

    [Fact]
    public async Task TransientHttpFailureRetriesThenReturnsSuccessfulResult()
    {
        var callCount = 0;
        var (service, client) = CreateClient((_, _) =>
        {
            callCount++;
            return Task.FromResult(callCount == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : JsonResponse(
                    """[{"lat":"6.9","lon":"79.8","display_name":"Colombo"}]"""));
        });
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.Found, result.Status);
            Assert.Equal(2, callCount);
        }
    }

    [Fact]
    public async Task RetryAfterIsRespectedForTransientRateLimit()
    {
        var callCount = 0;
        var firstAttemptAt = DateTimeOffset.UtcNow;
        var (service, client) = CreateClient((_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                firstAttemptAt = DateTimeOffset.UtcNow;
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter =
                    new System.Net.Http.Headers.RetryConditionHeaderValue(
                        TimeSpan.FromMilliseconds(200));
                return Task.FromResult(response);
            }

            return Task.FromResult(JsonResponse(
                """[{"lat":"6.9","lon":"79.8","display_name":"Colombo"}]"""));
        });
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.Found, result.Status);
            Assert.Equal(2, callCount);
            Assert.True(DateTimeOffset.UtcNow - firstAttemptAt >= TimeSpan.FromMilliseconds(180));
        }
    }

    [Fact]
    public async Task CancellationDuringRetryDelayPreventsNextAttempt()
    {
        var firstResponse = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        var (service, client) = CreateClient((_, _) =>
        {
            callCount++;
            firstResponse.TrySetResult();
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });
        using (client)
        using (var cancellation = new CancellationTokenSource())
        {
            var lookup = service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                cancellation.Token);
            await firstResponse.Task.WaitAsync(TimeSpan.FromSeconds(2));
            cancellation.Cancel();

            var result = await lookup;

            Assert.Equal(GeocodingStatus.Cancelled, result.Status);
            Assert.Equal(1, callCount);
        }
    }

    [Fact]
    public async Task TransientHttpRetriesStopAtConfiguredMaximum()
    {
        var callCount = 0;
        var (service, client) = CreateClient((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.ProviderUnavailable, result.Status);
            Assert.Equal("GEOCODING_HTTP_ERROR", result.ErrorCode);
            Assert.Equal(3, callCount);
        }
    }

    [Fact]
    public async Task PreCancelledCallerReturnsCancelledOutcomeWithoutCallingProvider()
    {
        var called = false;
        var (service, client) = CreateClient((_, _) =>
        {
            called = true;
            return Task.FromResult(JsonResponse("[]"));
        });
        using (client)
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "Colombo address" },
                cancellation.Token);

            Assert.Equal(GeocodingStatus.Cancelled, result.Status);
            Assert.Equal("GEOCODING_CANCELLED", result.ErrorCode);
            Assert.False(called);
        }
    }

    [Fact]
    public async Task InvalidInputReturnsExplicitOutcomeWithoutCallingProvider()
    {
        var called = false;
        var (service, client) = CreateClient((_, _) =>
        {
            called = true;
            return Task.FromResult(JsonResponse("[]"));
        });
        using (client)
        {
            var result = await service.ForwardAsync(
                new ForwardGeocodingRequest { Query = "  " },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.InvalidInput, result.Status);
            Assert.False(called);
        }
    }

    [Fact]
    public async Task InvalidReverseCoordinatesReturnInvalidInput()
    {
        var (service, client) = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("{}")));
        using (client)
        {
            var result = await service.ReverseAsync(
                new ReverseGeocodingRequest
                {
                    Latitude = 91,
                    Longitude = 0
                },
                CancellationToken.None);

            Assert.Equal(GeocodingStatus.InvalidInput, result.Status);
            Assert.Equal("GEOCODING_COORDINATES_INVALID", result.ErrorCode);
        }
    }

    private static (MappingClient Service, HttpClient Client) CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send,
        RecordingLogger? logger = null,
        TimeSpan? timeout = null,
        int timeoutSeconds = 5,
        int maximumRetries = 2)
    {
        var handler = new DelegateHandler(send);
        var client = new HttpClient(handler)
        {
            Timeout = timeout ?? Timeout.InfiniteTimeSpan
        };
        var options = new NominatimOptions
        {
            BaseUrl = "https://nominatim.test/",
            UserAgent = "HumanitarianAidPlatform.Tests/1.0 (contact: tests@example.org)",
            TimeoutSeconds = timeoutSeconds,
            MaximumCandidates = 5,
            MaximumRetries = maximumRetries
        };

        return (
            new MappingClient(
                client,
                Options.Create(options),
                logger ?? new RecordingLogger()),
            client);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json")
        };

    private sealed class DelegateHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }

    private sealed class RecordingLogger : ILogger<MappingClient>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
