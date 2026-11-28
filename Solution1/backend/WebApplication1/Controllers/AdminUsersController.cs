using HumanitarianAidPlatform.Application.Admin;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _service;

    public AdminUsersController(IAdminUserService service)
    {
        _service = service;
    }

    [HttpGet("pending-applications")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingAccountResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PendingAccountResponse>>> GetPendingApplications(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetPendingApplicationsAsync(cancellationToken, null));

    [HttpPost("{userId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveApplication(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _service.ApproveApplicationAsync(userId, cancellationToken);
        return result switch
        {
            ApplicantDecisionResult.Updated => NoContent(),
            ApplicantDecisionResult.NotFound => NotFound(),
            _ => Conflict()
        };
    }

    [HttpPost("{userId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectApplication(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _service.RejectApplicationAsync(userId, cancellationToken);
        return result switch
        {
            ApplicantDecisionResult.Updated => NoContent(),
            ApplicantDecisionResult.NotFound => NotFound(),
            _ => Conflict()
        };
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(AdminAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminAccountResponse>> GetAccount(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var account = await _service.GetAccountAsync(userId, cancellationToken);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost("staff")]
    [ProducesResponseType(typeof(AdminAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminAccountResponse>> CreateStaffAccount(
        CreateStaffAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateStaffAccountAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return result.Error switch
            {
                StaffAccountCreationError.EmailAlreadyExists =>
                    Problem(detail: "This email is already registered.", statusCode: StatusCodes.Status409Conflict),
                _ => Problem(detail: "The requested role is not a staff role.", statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return CreatedAtAction(
            nameof(GetAccount),
            new { userId = result.Account!.Id },
            result.Account);
    }
}
