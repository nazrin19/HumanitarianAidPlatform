using HumanitarianAidPlatform.Application.Volunteers;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/volunteers")]
[Authorize(Roles = $"{RoleNames.VolunteerCoordinator},{RoleNames.Admin}")]
public class VolunteersController : ControllerBase
{
    private readonly IVolunteerService _service;

    public VolunteersController(IVolunteerService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(VolunteerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VolunteerResponseDto>> Create(CreateVolunteerDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<List<VolunteerResponseDto>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VolunteerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VolunteerResponseDto>> GetById(Guid id)
    {
        var volunteer = await _service.GetByIdAsync(id);
        return volunteer is null ? NotFound() : Ok(volunteer);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VolunteerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VolunteerResponseDto>> Update(Guid id, CreateVolunteerDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id) =>
        await _service.DeleteAsync(id) ? NoContent() : NotFound();
}