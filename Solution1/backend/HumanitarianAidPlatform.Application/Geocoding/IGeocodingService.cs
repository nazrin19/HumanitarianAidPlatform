namespace HumanitarianAidPlatform.Application.Geocoding;

/// <summary>Converts between human-readable locations and geographic coordinates.</summary>
public interface IGeocodingService
{
    Task<GeocodingResult> ForwardAsync(
        ForwardGeocodingRequest request,
        CancellationToken cancellationToken);

    Task<GeocodingResult> ReverseAsync(
        ReverseGeocodingRequest request,
        CancellationToken cancellationToken);
}
