using HumanitarianAidPlatform.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        RegisterRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _auth.RegisterAsync(dto, cancellationToken);
        return result.Succeeded
            ? Ok(result.Response)
            : Problem(detail: result.Error, statusCode: StatusCodes.Status409Conflict);
    }

    [HttpPost("register/volunteer")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegistrationPendingResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationPendingResponse>> RegisterVolunteer(
        RegisterVolunteerRequest dto,
        CancellationToken cancellationToken)
    {
        var result = await _auth.RegisterVolunteerAsync(dto, cancellationToken);
        if (!result.Succeeded)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status409Conflict);

        return Accepted(new RegistrationPendingResponse(
            result.UserId!.Value,
            "Pending",
            "Your volunteer application was submitted and is awaiting administrator approval."));
    }

    [HttpPost("register/donor")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegistrationPendingResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationPendingResponse>> RegisterDonor(
        RegisterDonorRequest dto,
        CancellationToken cancellationToken)
    {
        var result = await _auth.RegisterDonorAsync(dto, cancellationToken);
        if (!result.Succeeded)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status409Conflict);

        return Accepted(new RegistrationPendingResponse(
            result.UserId!.Value,
            "Pending",
            "Your donor application was submitted and is awaiting administrator approval."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        LoginRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(dto, cancellationToken);
        return result.Succeeded
            ? Ok(result.Response)
            : Problem(detail: result.Error, statusCode: StatusCodes.Status401Unauthorized);
    }

    // Test endpoint: proves the token works and shows what the API reads from it.
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        Id = User.FindFirst("sub")?.Value,
        Email = User.FindFirst("email")?.Value,
        Name = User.FindFirst("name")?.Value,
        Role = User.FindFirst("role")?.Value
    });
}
