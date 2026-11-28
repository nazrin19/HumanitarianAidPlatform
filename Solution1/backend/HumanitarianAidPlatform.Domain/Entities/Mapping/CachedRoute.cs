namespace HumanitarianAidPlatform.Domain.Entities.Mapping;

public sealed class CachedRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CacheKey { get; set; } = string.Empty;
    public double OriginLatitude { get; set; }
    public double OriginLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public double DistanceKm { get; set; }
    public string RouteGeoJson { get; set; } = "{}";
    public DateTime CachedAtUtc { get; set; } = DateTime.UtcNow;
}
