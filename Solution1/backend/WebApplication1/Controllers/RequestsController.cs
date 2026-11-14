using HumanitarianAidPlatform.Api.Extensions;
using HumanitarianAidPlatform.Application.Requests;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private const string StaffRoles =
        RoleNames.Coordinator + "," +
        RoleNames.CaseManager + "," +
        RoleNames.ResourceManager + "," +
        RoleNames.ShelterAdmin + "," +
        RoleNames.Admin;

    private readonly IRequestService _service;

    public RequestsController(IRequestService service)
    {
        _service = service;
    }

    [HttpGet("mine")]
    [Authorize(Roles = RoleNames.Citizen)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] RequestListQuery query)
    {
        var userId = User.GetUserId();
        return userId is null
            ? Unauthorized()
            : Ok(await _service.GetMineAsync(userId.Value, query));
    }

    [HttpGet]
    [Authorize(Roles = StaffRoles)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForStaff([FromQuery] RequestListQuery query)
    {
        return Ok(await _service.GetForStaffAsync(query));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Citizen)]
    [ProducesResponseType(typeof(RequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RequestResponseDto>> Submit(CreateRequestDto dto)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var created = await _service.SubmitAsync(userId.Value, dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(RequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestResponseDto>> GetById(Guid id)
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        if (userId is null || role is null)
            return Unauthorized();

        var request = await _service.GetByIdAsync(id, userId.Value, role);
        return request is null ? NotFound() : Ok(request);
    }

    [HttpGet("{id:guid}/history")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<RequestStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RequestStatusHistoryDto>>> GetHistory(Guid id)
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        if (userId is null || role is null)
            return Unauthorized();

        var history = await _service.GetHistoryAsync(id, userId.Value, role);
        return history is null ? NotFound() : Ok(history);
    }

    [HttpGet("{id:guid}/recommendation")]
    [Authorize(Roles = $"{RoleNames.Coordinator},{RoleNames.CaseManager},{RoleNames.Admin}")]
    [ProducesResponseType(typeof(RecommendationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationDto>> GetRecommendation(Guid id)
    {
        var role = User.GetRole();
        if (role is null)
            return Unauthorized();

        var recommendation = await _service.GetRecommendationAsync(id, role);
        return recommendation is null ? NotFound() : Ok(recommendation);
    }
}