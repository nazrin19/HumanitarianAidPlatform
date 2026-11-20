using HumanitarianAidPlatform.Application.Volunteers;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/volunteer-tasks")]
[Authorize(Roles = $"{RoleNames.VolunteerCoordinator},{RoleNames.Admin}")]
public class VolunteerTasksController : ControllerBase
{
    private readonly IVolunteerTaskService _service;

    public VolunteerTasksController(IVolunteerTaskService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(VolunteerTaskResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VolunteerTaskResponseDto>> Create(CreateVolunteerTaskDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<List<VolunteerTaskResponseDto>>> GetAll(
        [FromQuery] string? status, [FromQuery] Guid? volunteerId) =>
        Ok(await _service.GetAllAsync(status, volunteerId));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VolunteerTaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VolunteerTaskResponseDto>> GetById(Guid id)
    {
        var task = await _service.GetByIdAsync(id);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPut("{id:guid}/assign")]
    [ProducesResponseType(typeof(VolunteerTaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VolunteerTaskResponseDto>> Assign(Guid id, AssignVolunteerTaskDto dto)
    {
        try
        {
            var task = await _service.AssignAsync(id, dto.VolunteerId);
            return task is null ? NotFound() : Ok(task);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/complete")]
    [ProducesResponseType(typeof(VolunteerTaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VolunteerTaskResponseDto>> Complete(Guid id)
    {
        try
        {
            var task = await _service.CompleteAsync(id);
            return task is null ? NotFound() : Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}