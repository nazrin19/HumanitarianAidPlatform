using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HumanitarianAidPlatform.Application.UserViews;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/user-views")]
public class UserViewsController : ControllerBase
{
    private readonly IUserViewService _service;

    public UserViewsController(IUserViewService service)
    {
        _service = service;
    }

    [HttpGet("donor")]
    [Authorize(Roles = RoleNames.Donor)]
    [ProducesResponseType(typeof(DonorUserViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DonorUserViewDto>> GetDonorView()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var view = await _service.GetDonorViewAsync(userId.Value);
        return view is null ? NotFound() : Ok(view);
    }

    [HttpGet("volunteer")]
    [Authorize(Roles = RoleNames.Volunteer)]
    [ProducesResponseType(typeof(VolunteerUserViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VolunteerUserViewDto>> GetVolunteerView()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var view = await _service.GetVolunteerViewAsync(userId.Value);
        return view is null ? NotFound() : Ok(view);
    }

    private Guid? GetUserId()
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}
