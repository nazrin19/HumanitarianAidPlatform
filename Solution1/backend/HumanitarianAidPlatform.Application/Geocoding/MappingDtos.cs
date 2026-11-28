using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Geocoding;

/// <summary>Geographic coordinate pair used by mapping operations.</summary>
public sealed record MapPoint
{
    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }
}

/// <summary>Origin and destination for a route or straight-line distance lookup.</summary>
public sealed record MapRouteRequest
{
    [Required]
    public required MapPoint Origin { get; init; }

    [Required]
    public required MapPoint Destination { get; init; }
}

/// <summary>Route shape and distance; the fallback route is a direct line.</summary>
public sealed record MapRouteResponse(
    double DistanceKm,
    string RouteGeoJson,
    bool IsFallback,
    string Message);

/// <summary>Named location saved as a reusable map reference.</summary>
public sealed record MapReferenceResponse(
    Guid Id,
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    DateTimeOffset CreatedAtUtc);

/// <summary>Payload for saving a named map reference.</summary>
public sealed record CreateMapReferenceRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    public required string Name { get; init; }

    [StringLength(300)]
    public string? Address { get; init; }

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }
}

/// <summary>Named GeoJSON service area shown as a map layer.</summary>
public sealed record ServiceAreaResponse(
    Guid Id,
    string Name,
    string GeoJson,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

/// <summary>Payload for saving a GeoJSON service area.</summary>
public sealed record CreateServiceAreaRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(500000, MinimumLength = 2)]
    public required string GeoJson { get; init; }

    public bool IsActive { get; init; } = true;
}
