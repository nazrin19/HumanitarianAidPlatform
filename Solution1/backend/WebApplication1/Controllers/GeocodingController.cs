using HumanitarianAidPlatform.Application.Geocoding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

/// <summary>Provides authenticated location lookup endpoints.</summary>
[ApiController]
[Route("api/geocoding")]
[Authorize]
public sealed class GeocodingController : ControllerBase
{
    private readonly IGeocodingService _geocodingService;

    public GeocodingController(IGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    /// <summary>Finds candidate coordinates and formatted addresses for location text.</summary>
    [HttpPost("forward")]
    [ProducesResponseType(typeof(GeocodingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GeocodingResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GeocodingResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GeocodingResult), 499)]
    [ProducesResponseType(typeof(GeocodingResult), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GeocodingResult>> Forward(
        [FromBody] ForwardGeocodingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _geocodingService.ForwardAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Finds a formatted address for geographic coordinates.</summary>
    [HttpPost("reverse")]
    [ProducesResponseType(typeof(GeocodingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GeocodingResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GeocodingResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GeocodingResult), 499)]
    [ProducesResponseType(typeof(GeocodingResult), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GeocodingResult>> Reverse(
        [FromBody] ReverseGeocodingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _geocodingService.ReverseAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<GeocodingResult> ToActionResult(GeocodingResult result)
    {
        return result.Status switch
        {
            GeocodingStatus.InvalidInput => BadRequest(result),
            GeocodingStatus.NotFound => NotFound(result),
            GeocodingStatus.ProviderUnavailable =>
                StatusCode(StatusCodes.Status503ServiceUnavailable, result),
            GeocodingStatus.Cancelled => StatusCode(499, result),
            _ => Ok(result)
        };
    }
}
