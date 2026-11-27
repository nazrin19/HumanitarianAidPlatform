using System.Security.Claims;
using HumanitarianAidPlatform.Application.Donors;
using HumanitarianAidPlatform.Application.Volunteers;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IDonorService _donors;
    private readonly IVolunteerService _volunteers;

    public ProfileController(IDonorService donors, IVolunteerService volunteers)
    {
        _donors = donors;
        _volunteers = volunteers;
    }

    [HttpGet("donor")]
    [Authorize(Roles = RoleNames.Donor)]
    public async Task<ActionResult<DonorResponseDto>> GetDonorProfile()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var profile = await _donors.GetByUserIdAsync(userId.Value);
        return profile is null ? NotFound("Donor profile was not found.") : Ok(profile);
    }

    [HttpPut("donor")]
    [Authorize(Roles = RoleNames.Donor)]
    public async Task<ActionResult<DonorResponseDto>> UpdateDonorProfile(
        UpdateDonorProfileDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _donors.UpdateMyProfileAsync(userId.Value, dto);
            return profile is null
                ? NotFound("Donor profile was not found.")
                : Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("volunteer")]
    [Authorize(Roles = RoleNames.Volunteer)]
    public async Task<ActionResult<VolunteerResponseDto>> GetVolunteerProfile()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var profile = await _volunteers.GetByUserIdAsync(userId.Value);
        return profile is null ? NotFound("Volunteer profile was not found.") : Ok(profile);
    }

    [HttpPut("volunteer")]
    [Authorize(Roles = RoleNames.Volunteer)]
    public async Task<ActionResult<VolunteerResponseDto>> UpdateVolunteerProfile(
        UpdateVolunteerProfileDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _volunteers.UpdateMyProfileAsync(userId.Value, dto);
            return profile is null
                ? NotFound("Volunteer profile was not found.")
                : Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
