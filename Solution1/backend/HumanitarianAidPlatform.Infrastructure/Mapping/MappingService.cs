using System.Globalization;
using System.Text.Json;
using HumanitarianAidPlatform.Application.Geocoding;
using HumanitarianAidPlatform.Domain.Entities.Mapping;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HumanitarianAidPlatform.Infrastructure.Mapping;

public sealed class MappingService : IMappingService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _memoryCache;

    public MappingService(AppDbContext db, IMemoryCache memoryCache)
    {
        _db = db;
        _memoryCache = memoryCache;
    }

    public async Task<MapRouteResponse> GetRouteAsync(
        MapRouteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidatePoint(request.Origin, nameof(request.Origin));
        ValidatePoint(request.Destination, nameof(request.Destination));

        var cacheKey = CreateRouteKey(request.Origin, request.Destination);
        if (_memoryCache.TryGetValue(cacheKey, out MapRouteResponse? cachedInMemory) &&
            cachedInMemory is not null)
        {
            return cachedInMemory;
        }

        var cachedInDatabase = await _db.CachedRoutes
            .AsNoTracking()
            .FirstOrDefaultAsync(route => route.CacheKey == cacheKey, cancellationToken);
        if (cachedInDatabase is not null)
        {
            var result = new MapRouteResponse(
                cachedInDatabase.DistanceKm,
                cachedInDatabase.RouteGeoJson,
                true,
                "Showing a cached straight-line route estimate.");
            _memoryCache.Set(cacheKey, result, TimeSpan.FromHours(6));
            return result;
        }

        var distanceKm = CalculateDistanceKm(
            request.Origin.Latitude,
            request.Origin.Longitude,
            request.Destination.Latitude,
            request.Destination.Longitude);
        var routeGeoJson = JsonSerializer.Serialize(new
        {
            type = "LineString",
            coordinates = new[]
            {
                new[] { request.Origin.Longitude, request.Origin.Latitude },
                new[] { request.Destination.Longitude, request.Destination.Latitude }
            }
        });
        var route = new CachedRoute
        {
            CacheKey = cacheKey,
            OriginLatitude = request.Origin.Latitude,
            OriginLongitude = request.Origin.Longitude,
            DestinationLatitude = request.Destination.Latitude,
            DestinationLongitude = request.Destination.Longitude,
            DistanceKm = distanceKm,
            RouteGeoJson = routeGeoJson,
            CachedAtUtc = DateTime.UtcNow
        };
        await _db.CachedRoutes.AddAsync(route, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new MapRouteResponse(
            distanceKm,
            routeGeoJson,
            true,
            "Showing a straight-line distance and route estimate; road routing is unavailable.");
        _memoryCache.Set(cacheKey, response, TimeSpan.FromHours(6));
        return response;
    }

    public async Task<IReadOnlyList<MapReferenceResponse>> GetReferencesAsync(
        CancellationToken cancellationToken)
    {
        var references = await _db.MapReferences
            .AsNoTracking()
            .OrderBy(reference => reference.Name)
            .ToListAsync(cancellationToken);
        return references.Select(ToResponse).ToArray();
    }

    public async Task<MapReferenceResponse?> GetReferenceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reference = await _db.MapReferences
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return reference is null ? null : ToResponse(reference);
    }

    public async Task<MapReferenceResponse> CreateReferenceAsync(
        CreateMapReferenceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidatePoint(
            new MapPoint
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude
            },
            nameof(request));
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A reference name is required.", nameof(request));
        }

        var reference = new MapReference
        {
            Name = request.Name.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address)
                ? null
                : request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _db.MapReferences.AddAsync(reference, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(reference);
    }

    public async Task<IReadOnlyList<ServiceAreaResponse>> GetServiceAreasAsync(
        CancellationToken cancellationToken)
    {
        var areas = await _db.ServiceAreas
            .AsNoTracking()
            .Where(area => area.IsActive)
            .OrderBy(area => area.Name)
            .ToListAsync(cancellationToken);
        return areas.Select(ToResponse).ToArray();
    }

    public async Task<ServiceAreaResponse?> GetServiceAreaAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var area = await _db.ServiceAreas
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);
        return area is null ? null : ToResponse(area);
    }

    public async Task<ServiceAreaResponse> CreateServiceAreaAsync(
        CreateServiceAreaRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A service area name is required.", nameof(request));
        }

        ValidateGeoJson(request.GeoJson);
        var area = new ServiceArea
        {
            Name = request.Name.Trim(),
            GeoJson = request.GeoJson,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _db.ServiceAreas.AddAsync(area, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(area);
    }

    public static double CalculateDistanceKm(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        const double earthRadiusKm = 6371.0088;
        var lat1 = DegreesToRadians(latitude1);
        var lat2 = DegreesToRadians(latitude2);
        var deltaLatitude = DegreesToRadians(latitude2 - latitude1);
        var deltaLongitude = DegreesToRadians(longitude2 - longitude1);
        var a = Math.Pow(Math.Sin(deltaLatitude / 2), 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Pow(Math.Sin(deltaLongitude / 2), 2);
        return earthRadiusKm * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    private static void ValidatePoint(MapPoint? point, string parameterName)
    {
        if (point is null ||
            !double.IsFinite(point.Latitude) ||
            point.Latitude is < -90 or > 90 ||
            !double.IsFinite(point.Longitude) ||
            point.Longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Valid latitude and longitude coordinates are required.");
        }
    }

    private static string CreateRouteKey(MapPoint origin, MapPoint destination) =>
        string.Join(
            ":",
            origin.Latitude.ToString("F5", CultureInfo.InvariantCulture),
            origin.Longitude.ToString("F5", CultureInfo.InvariantCulture),
            destination.Latitude.ToString("F5", CultureInfo.InvariantCulture),
            destination.Longitude.ToString("F5", CultureInfo.InvariantCulture));

    private static void ValidateGeoJson(string geoJson)
    {
        try
        {
            using var document = JsonDocument.Parse(geoJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("type", out var type) ||
                type.ValueKind != JsonValueKind.String ||
                type.GetString() is not ("Polygon" or "MultiPolygon" or "Feature" or "FeatureCollection"))
            {
                throw new ArgumentException(
                    "Service areas must be valid GeoJSON polygon data.",
                    nameof(geoJson));
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Service areas must contain valid GeoJSON.",
                nameof(geoJson),
                exception);
        }
    }

    private static MapReferenceResponse ToResponse(MapReference reference) =>
        new(
            reference.Id,
            reference.Name,
            reference.Address,
            reference.Latitude,
            reference.Longitude,
            new DateTimeOffset(DateTime.SpecifyKind(
                reference.CreatedAtUtc,
                DateTimeKind.Utc)));

    private static ServiceAreaResponse ToResponse(ServiceArea area) =>
        new(
            area.Id,
            area.Name,
            area.GeoJson,
            area.IsActive,
            new DateTimeOffset(DateTime.SpecifyKind(
                area.CreatedAtUtc,
                DateTimeKind.Utc)));
}
