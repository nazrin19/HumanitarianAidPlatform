using HumanitarianAidPlatform.Application.Geocoding;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

/// <summary>Provides route estimates and reusable map layers.</summary>
[ApiController]
[Route("api/mapping")]
[Authorize]
public sealed class MappingController : ControllerBase
{
    private readonly IMappingService _mappingService;

    public MappingController(IMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <summary>Returns a cached straight-line route and Haversine distance estimate.</summary>
    [HttpPost("routes")]
    [ProducesResponseType(typeof(MapRouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MapRouteResponse>> GetRoute(
        [FromBody] MapRouteRequest request,
        CancellationToken cancellationToken)
    {
        var route = await _mappingService.GetRouteAsync(request, cancellationToken);
        return Ok(route);
    }

    /// <summary>Lists saved map references.</summary>
    [HttpGet("references")]
    [ProducesResponseType(typeof(IReadOnlyList<MapReferenceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MapReferenceResponse>>> GetReferences(
        CancellationToken cancellationToken) =>
        Ok(await _mappingService.GetReferencesAsync(cancellationToken));

    /// <summary>Returns one saved map reference.</summary>
    [HttpGet("references/{id:guid}")]
    [ProducesResponseType(typeof(MapReferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MapReferenceResponse>> GetReference(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reference = await _mappingService.GetReferenceAsync(id, cancellationToken);
        return reference is null ? NotFound() : Ok(reference);
    }

    /// <summary>Saves a named map reference.</summary>
    [HttpPost("references")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Coordinator},{RoleNames.ResourceManager}")]
    [ProducesResponseType(typeof(MapReferenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MapReferenceResponse>> CreateReference(
        [FromBody] CreateMapReferenceRequest request,
        CancellationToken cancellationToken)
    {
        var reference = await _mappingService.CreateReferenceAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetReference),
            new { id = reference.Id },
            reference);
    }

    /// <summary>Lists active GeoJSON service areas.</summary>
    [HttpGet("service-areas")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceAreaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceAreaResponse>>> GetServiceAreas(
        CancellationToken cancellationToken) =>
        Ok(await _mappingService.GetServiceAreasAsync(cancellationToken));

    /// <summary>Returns one active service area.</summary>
    [HttpGet("service-areas/{id:guid}")]
    [ProducesResponseType(typeof(ServiceAreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceAreaResponse>> GetServiceArea(
        Guid id,
        CancellationToken cancellationToken)
    {
        var area = await _mappingService.GetServiceAreaAsync(id, cancellationToken);
        return area is null ? NotFound() : Ok(area);
    }

    /// <summary>Saves a named GeoJSON service area.</summary>
    [HttpPost("service-areas")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Coordinator}")]
    [ProducesResponseType(typeof(ServiceAreaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceAreaResponse>> CreateServiceArea(
        [FromBody] CreateServiceAreaRequest request,
        CancellationToken cancellationToken)
    {
        var area = await _mappingService.CreateServiceAreaAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetServiceArea),
            new { id = area.Id },
            area);
    }
}
