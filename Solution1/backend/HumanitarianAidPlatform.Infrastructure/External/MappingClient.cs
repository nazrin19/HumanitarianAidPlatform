using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HumanitarianAidPlatform.Application.Geocoding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HumanitarianAidPlatform.Infrastructure.External;

public sealed class MappingClient : IGeocodingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly Uri _baseAddress;
    private readonly NominatimOptions _options;
    private readonly ILogger<MappingClient> _logger;

    public MappingClient(
        HttpClient httpClient,
        IOptions<NominatimOptions> options,
        ILogger<MappingClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        var baseUrl = _options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? _options.BaseUrl
            : $"{_options.BaseUrl}/";
        _baseAddress = new Uri(baseUrl, UriKind.Absolute);
    }

    public Task<GeocodingResult> ForwardAsync(
        ForwardGeocodingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = request.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length is < 4 or > 300)
        {
            return Task.FromResult(InvalidInput(
                "GEOCODING_QUERY_INVALID",
                "Enter a location description between 4 and 300 characters."));
        }

        var uri = new Uri(
            _baseAddress,
            $"search?format=jsonv2&limit={Math.Clamp(_options.MaximumCandidates, 2, 10)}&q={Uri.EscapeDataString(query)}");

        return ExecuteAsync(
            "forward",
            cancellationToken,
            async requestToken =>
            {
                using var response = await _httpClient.GetAsync(
                    uri,
                    HttpCompletionOption.ResponseHeadersRead,
                    requestToken);

                if (!response.IsSuccessStatusCode)
                {
                    return HttpFailure(response, "forward");
                }

                var places = await response.Content.ReadFromJsonAsync<List<NominatimPlace>>(
                    JsonOptions,
                    requestToken);

                if (places is null)
                {
                    throw new JsonException("Nominatim returned an empty JSON document.");
                }

                if (places.Count == 0)
                {
                    return NotFound();
                }

                var candidates = places.Select(ParseCandidate).ToArray();
                return MatchResult(candidates);
            });
    }

    public Task<GeocodingResult> ReverseAsync(
        ReverseGeocodingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Latitude is not double latitude ||
            request.Longitude is not double longitude ||
            !double.IsFinite(latitude) ||
            !double.IsFinite(longitude) ||
            latitude is < -90 or > 90 ||
            longitude is < -180 or > 180)
        {
            return Task.FromResult(InvalidInput(
                "GEOCODING_COORDINATES_INVALID",
                "Provide valid latitude and longitude coordinates."));
        }

        var uri = new Uri(
            _baseAddress,
            $"reverse?format=jsonv2&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}");

        return ExecuteAsync(
            "reverse",
            cancellationToken,
            async requestToken =>
            {
                using var response = await _httpClient.GetAsync(
                    uri,
                    HttpCompletionOption.ResponseHeadersRead,
                    requestToken);

                if (!response.IsSuccessStatusCode)
                {
                    return HttpFailure(response, "reverse");
                }

                var place = await response.Content.ReadFromJsonAsync<NominatimPlace>(
                    JsonOptions,
                    requestToken);

                if (place is null)
                {
                    throw new JsonException("Nominatim returned an empty JSON document.");
                }

                if (!string.IsNullOrWhiteSpace(place.Error))
                {
                    return NotFound();
                }

                var candidate = ParseCandidate(place);
                return MatchResult([candidate]);
            });
    }

    private async Task<GeocodingResult> ExecuteAsync(
        string operation,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task<GeocodingResult>> action)
    {
        var maximumAttempts = Math.Clamp(_options.MaximumRetries, 0, 2) + 1;
        using var overallTimeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        overallTimeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        var perAttemptTimeout = TimeSpan.FromTicks(
            Math.Max(
                1,
                TimeSpan.FromSeconds(_options.TimeoutSeconds).Ticks / maximumAttempts));

        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            using var timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(overallTimeoutSource.Token);
            timeoutSource.CancelAfter(perAttemptTimeout);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await action(timeoutSource.Token);
                _logger.LogInformation(
                    "Nominatim {Operation} completed with {Status} after {AttemptCount} attempts",
                    operation,
                    result.Status,
                    attempt);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return LogCancelled(operation);
            }
            catch (OperationCanceledException) when (overallTimeoutSource.IsCancellationRequested)
            {
                return LogTimeout(operation, attempt);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Nominatim {Operation} timed out on attempt {AttemptCount}",
                    operation,
                    attempt);

                if (attempt < maximumAttempts)
                {
                    if (await WaitBeforeRetryAsync(
                            Backoff(attempt),
                            overallTimeoutSource.Token))
                    {
                        continue;
                    }

                    return CallerCancelledOrTimeout(
                        operation,
                        attempt,
                        cancellationToken,
                        overallTimeoutSource.Token);
                }

                return Failure(
                    GeocodingStatus.ProviderUnavailable,
                    "GEOCODING_TIMEOUT",
                    "The location provider did not respond in time.");
            }
            catch (TransientHttpException exception)
            {
                _logger.LogWarning(
                    "Nominatim {Operation} returned transient HTTP status {StatusCode} on attempt {AttemptCount}",
                    operation,
                    (int)exception.StatusCode,
                    attempt);

                var delay = RetryDelay(attempt, exception.RetryAfter);
                if (attempt < maximumAttempts && delay is TimeSpan retryDelay)
                {
                    if (await WaitBeforeRetryAsync(
                            retryDelay,
                            overallTimeoutSource.Token))
                    {
                        continue;
                    }

                    return CallerCancelledOrTimeout(
                        operation,
                        attempt,
                        cancellationToken,
                        overallTimeoutSource.Token);
                }

                return HttpFailure(exception.StatusCode, operation);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(
                    "Nominatim {Operation} network request failed on attempt {AttemptCount} ({ExceptionType})",
                    operation,
                    attempt,
                    exception.GetType().Name);

                if (attempt < maximumAttempts &&
                    await WaitBeforeRetryAsync(
                        Backoff(attempt),
                        overallTimeoutSource.Token))
                {
                    continue;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return LogCancelled(operation);
                }
                if (overallTimeoutSource.IsCancellationRequested)
                {
                    return LogTimeout(operation, attempt);
                }

                return Failure(
                    GeocodingStatus.ProviderUnavailable,
                    "GEOCODING_NETWORK_ERROR",
                    "The location provider is currently unavailable.");
            }
            catch (IOException exception)
            {
                _logger.LogWarning(
                    "Nominatim {Operation} response stream failed on attempt {AttemptCount} ({ExceptionType})",
                    operation,
                    attempt,
                    exception.GetType().Name);

                if (attempt < maximumAttempts &&
                    await WaitBeforeRetryAsync(
                        Backoff(attempt),
                        overallTimeoutSource.Token))
                {
                    continue;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return LogCancelled(operation);
                }
                if (overallTimeoutSource.IsCancellationRequested)
                {
                    return LogTimeout(operation, attempt);
                }

                return Failure(
                    GeocodingStatus.ProviderUnavailable,
                    "GEOCODING_NETWORK_ERROR",
                    "The location provider is currently unavailable.");
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    "Nominatim {Operation} returned an invalid response ({ExceptionType})",
                    operation,
                    exception.GetType().Name);
                return Failure(
                    GeocodingStatus.ProviderUnavailable,
                    "GEOCODING_INVALID_RESPONSE",
                    "The location provider returned an invalid response.");
            }
            catch (UriFormatException exception)
            {
                _logger.LogError(
                    "Nominatim {Operation} URL configuration is invalid ({ExceptionType})",
                    operation,
                    exception.GetType().Name);
                return Failure(
                    GeocodingStatus.ProviderUnavailable,
                    "GEOCODING_CONFIGURATION_ERROR",
                    "Location lookup is not configured correctly.");
            }
        }

        throw new InvalidOperationException("The geocoding retry loop exited unexpectedly.");
    }

    private GeocodingResult HttpFailure(HttpResponseMessage response, string operation)
    {
        var statusCode = response.StatusCode;
        if (IsTransient(statusCode))
        {
            throw new TransientHttpException(
                statusCode,
                GetRetryAfter(response));
        }

        return HttpFailure(statusCode, operation);
    }

    private GeocodingResult HttpFailure(HttpStatusCode statusCode, string operation)
    {
        _logger.LogWarning(
            "Nominatim {Operation} failed with {ErrorCode} and HTTP status {StatusCode}",
            operation,
            "GEOCODING_HTTP_ERROR",
            (int)statusCode);
        return Failure(
            GeocodingStatus.ProviderUnavailable,
            "GEOCODING_HTTP_ERROR",
            "The location provider could not complete the lookup.");
    }

    private GeocodingResult LogCancelled(string operation)
    {
        _logger.LogInformation("Nominatim {Operation} was cancelled", operation);
        return Failure(
            GeocodingStatus.Cancelled,
            "GEOCODING_CANCELLED",
            "The location lookup was cancelled.");
    }

    private GeocodingResult LogTimeout(string operation, int attempt)
    {
        _logger.LogWarning(
            "Nominatim {Operation} failed with {ErrorCode} after {AttemptCount} attempts",
            operation,
            "GEOCODING_TIMEOUT",
            attempt);
        return Failure(
            GeocodingStatus.ProviderUnavailable,
            "GEOCODING_TIMEOUT",
            "The location provider did not respond in time.");
    }

    private GeocodingResult CallerCancelledOrTimeout(
        string operation,
        int attempt,
        CancellationToken callerToken,
        CancellationToken overallTimeoutToken) =>
        callerToken.IsCancellationRequested
            ? LogCancelled(operation)
            : overallTimeoutToken.IsCancellationRequested
                ? LogTimeout(operation, attempt)
                : Failure(
                    GeocodingStatus.ProviderUnavailable,
                    "GEOCODING_TIMEOUT",
                    "The location provider did not respond in time.");

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is TimeSpan delta)
        {
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        if (retryAfter?.Date is DateTimeOffset date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }

    private static TimeSpan Backoff(int failedAttempt) =>
        TimeSpan.FromMilliseconds(
            Math.Min(250 * Math.Pow(2, failedAttempt - 1), 1000) +
            Random.Shared.Next(0, 101));

    private static TimeSpan? RetryDelay(int failedAttempt, TimeSpan? retryAfter)
    {
        var delay = Backoff(failedAttempt);
        if (retryAfter is TimeSpan requestedDelay)
        {
            if (requestedDelay > TimeSpan.FromSeconds(5))
            {
                return null;
            }

            if (requestedDelay > delay)
            {
                delay = requestedDelay;
            }
        }

        return delay;
    }

    private static async Task<bool> WaitBeforeRetryAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private sealed class TransientHttpException(
        HttpStatusCode statusCode,
        TimeSpan? retryAfter) : Exception
    {
        public HttpStatusCode StatusCode { get; } = statusCode;
        public TimeSpan? RetryAfter { get; } = retryAfter;
    }

    private static GeocodingCandidate ParseCandidate(NominatimPlace? place)
    {
        if (place is null ||
            !double.TryParse(
                place.Latitude,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var latitude) ||
            !double.TryParse(
                place.Longitude,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var longitude) ||
            !double.IsFinite(latitude) ||
            !double.IsFinite(longitude) ||
            latitude is < -90 or > 90 ||
            longitude is < -180 or > 180 ||
            string.IsNullOrWhiteSpace(place.DisplayAddress))
        {
            throw new JsonException("Nominatim returned an invalid location candidate.");
        }

        return new GeocodingCandidate(
            latitude,
            longitude,
            place.DisplayAddress);
    }

    private static GeocodingResult MatchResult(
        IReadOnlyList<GeocodingCandidate> candidates)
    {
        if (candidates.Count > 1)
        {
            return new GeocodingResult(
                GeocodingStatus.Ambiguous,
                null,
                null,
                null,
                candidates,
                true,
                "GEOCODING_AMBIGUOUS",
                "More than one location matched. Select a candidate.");
        }

        var match = candidates[0];
        return new GeocodingResult(
            GeocodingStatus.Found,
            match.Latitude,
            match.Longitude,
            match.DisplayAddress,
            candidates,
            false,
            null,
            null);
    }

    private static GeocodingResult NotFound() =>
        new(
            GeocodingStatus.NotFound,
            null,
            null,
            null,
            [],
            false,
            null,
            "No matching location was found.");

    private static GeocodingResult InvalidInput(string code, string message) =>
        Failure(GeocodingStatus.InvalidInput, code, message);

    private static GeocodingResult Failure(
        GeocodingStatus status,
        string errorCode,
        string errorMessage) =>
        new(
            status,
            null,
            null,
            null,
            [],
            false,
            errorCode,
            errorMessage);

    private sealed class NominatimPlace
    {
        [JsonPropertyName("lat")]
        public string? Latitude { get; init; }

        [JsonPropertyName("lon")]
        public string? Longitude { get; init; }

        [JsonPropertyName("display_name")]
        public string? DisplayAddress { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }
}
