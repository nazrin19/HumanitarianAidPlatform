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
                    return HttpFailure(response.StatusCode, "forward");
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
                    return HttpFailure(response.StatusCode, "reverse");
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
        var startedAt = DateTimeOffset.UtcNow;
        using var timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await action(timeoutSource.Token);
            _logger.LogInformation(
                "Nominatim {Operation} completed with {Status}",
                operation,
                result.Status);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Nominatim {Operation} was cancelled",
                operation);
            return Failure(
                GeocodingStatus.Cancelled,
                "GEOCODING_CANCELLED",
                "The location lookup was cancelled.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Nominatim {Operation} timed out after {ElapsedMilliseconds} ms",
                operation,
                (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
            return Failure(
                GeocodingStatus.ProviderUnavailable,
                "GEOCODING_TIMEOUT",
                "The location provider did not respond in time.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                "Nominatim {Operation} network request failed with {ExceptionType}",
                operation,
                exception.GetType().Name);
            return Failure(
                GeocodingStatus.ProviderUnavailable,
                "GEOCODING_NETWORK_ERROR",
                "The location provider is currently unavailable.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                "Nominatim {Operation} response stream failed with {ExceptionType}",
                operation,
                exception.GetType().Name);
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

    private GeocodingResult HttpFailure(HttpStatusCode statusCode, string operation)
    {
        _logger.LogWarning(
            "Nominatim {Operation} returned HTTP status {StatusCode}",
            operation,
            (int)statusCode);
        return Failure(
            GeocodingStatus.ProviderUnavailable,
            "GEOCODING_HTTP_ERROR",
            "The location provider could not complete the lookup.");
    }

    private static GeocodingCandidate ParseCandidate(NominatimPlace place)
    {
        if (!double.TryParse(
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
