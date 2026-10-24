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
    private readonly IRequestService _service;

    public RequestsController(IRequestService service)
    {
        _service = service;
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
}