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
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto dto)
    {
        var result = await _auth.RegisterAsync(dto);
        return result.Succeeded
            ? Ok(result.Response)
            : Problem(detail: result.Error, statusCode: StatusCodes.Status409Conflict);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto)
    {
        var result = await _auth.LoginAsync(dto);
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
