using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Geocoding;

/// <summary>Input for looking up coordinates from a human-readable location.</summary>
public sealed record ForwardGeocodingRequest
{
    [Required]
    [StringLength(300, MinimumLength = 4)]
    public required string Query { get; init; }
}

/// <summary>Input for looking up a human-readable location from coordinates.</summary>
public sealed record ReverseGeocodingRequest
{
    [Required]
    [Range(-90, 90)]
    public double? Latitude { get; init; }

    [Required]
    [Range(-180, 180)]
    public double? Longitude { get; init; }
}

/// <summary>A possible location match returned by the geocoding provider.</summary>
public sealed record GeocodingCandidate(
    double Latitude,
    double Longitude,
    string DisplayAddress);

/// <summary>Describes a geocoding outcome, selected location, and any candidate matches.</summary>
public sealed record GeocodingResult(
    GeocodingStatus Status,
    double? Latitude,
    double? Longitude,
    string? DisplayAddress,
    IReadOnlyList<GeocodingCandidate> Candidates,
    bool IsAmbiguous,
    string? ErrorCode,
    string? ErrorMessage);
