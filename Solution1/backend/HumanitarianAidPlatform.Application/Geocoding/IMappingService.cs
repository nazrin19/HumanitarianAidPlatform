namespace HumanitarianAidPlatform.Application.Geocoding;

/// <summary>Provides reusable mapping references, service areas, and route helpers.</summary>
public interface IMappingService
{
    Task<MapRouteResponse> GetRouteAsync(
        MapRouteRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MapReferenceResponse>> GetReferencesAsync(
        CancellationToken cancellationToken);

    Task<MapReferenceResponse?> GetReferenceAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<MapReferenceResponse> CreateReferenceAsync(
        CreateMapReferenceRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceAreaResponse>> GetServiceAreasAsync(
        CancellationToken cancellationToken);

    Task<ServiceAreaResponse?> GetServiceAreaAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ServiceAreaResponse> CreateServiceAreaAsync(
        CreateServiceAreaRequest request,
        CancellationToken cancellationToken);
}
