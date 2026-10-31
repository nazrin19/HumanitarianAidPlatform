using HumanitarianAidPlatform.Application.Shelters;
using HumanitarianAidPlatform.Application.Shelters.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/shelters")]
public class SheltersController : ControllerBase
{
    private readonly ShelterService _service;

    public SheltersController(ShelterService service)
    {
        _service = service;
    }

    // GET: api/shelters
    [HttpGet]
    public async Task<ActionResult<List<ShelterDto>>> GetAll()
    {
        var shelters = await _service.GetAllAsync();
        return Ok(shelters);
    }

    // GET: api/shelters/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShelterDto>> GetById(Guid id)
    {
        var shelter = await _service.GetByIdAsync(id);

        if (shelter is null)
            return NotFound();

        return Ok(shelter);
    }

    // POST: api/shelters
    [HttpPost]
    public async Task<ActionResult<ShelterDto>> Create(
        [FromBody] ShelterDto dto)
    {
        var created = await _service.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    // PUT: api/shelters/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ShelterDto>> Update(
        Guid id,
        [FromBody] ShelterDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);

        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    // DELETE: api/shelters/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // GET: api/shelters/recommendations
    [HttpGet("recommendations")]
    public async Task<ActionResult<List<ShelterRecommendationDto>>>
        GetRecommendations(
            [FromQuery] double? latitude,
            [FromQuery] double? longitude,
            [FromQuery] int requiredCapacity = 1)
    {
        if (requiredCapacity < 1)
        {
            return BadRequest(
                "Required capacity must be at least 1.");
        }

        if (latitude.HasValue &&
            (latitude.Value < -90 || latitude.Value > 90))
        {
            return BadRequest(
                "Latitude must be between -90 and 90.");
        }

        if (longitude.HasValue &&
            (longitude.Value < -180 || longitude.Value > 180))
        {
            return BadRequest(
                "Longitude must be between -180 and 180.");
        }

        if (latitude.HasValue != longitude.HasValue)
        {
            return BadRequest(
                "Latitude and longitude must be provided together.");
        }

        var recommendations =
            await _service.GetRecommendationsAsync(
                latitude,
                longitude,
                requiredCapacity);

        return Ok(recommendations);
    }
}