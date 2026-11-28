using HumanitarianAidPlatform.Application.Admin;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IAdminUserService _service;

    public ApplicationsController(IAdminUserService service)
    {
        _service = service;
    }

    [HttpGet("donors/pending")]
    [Authorize(Roles = $"{RoleNames.DonorOfficer},{RoleNames.Admin}")]
    public async Task<ActionResult<IReadOnlyList<PendingAccountResponse>>> GetPendingDonors(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetPendingApplicationsAsync(
            cancellationToken, RoleNames.Donor));

    [HttpPost("donors/{userId:guid}/approve")]
    [Authorize(Roles = $"{RoleNames.DonorOfficer},{RoleNames.Admin}")]
    public Task<IActionResult> ApproveDonor(
        Guid userId,
        CancellationToken cancellationToken) =>
        DecideAsync(userId, RoleNames.Donor, true, cancellationToken);

    [HttpPost("donors/{userId:guid}/reject")]
    [Authorize(Roles = $"{RoleNames.DonorOfficer},{RoleNames.Admin}")]
    public Task<IActionResult> RejectDonor(
        Guid userId,
        CancellationToken cancellationToken) =>
        DecideAsync(userId, RoleNames.Donor, false, cancellationToken);

    [HttpGet("volunteers/pending")]
    [Authorize(Roles = $"{RoleNames.VolunteerCoordinator},{RoleNames.Admin}")]
    public async Task<ActionResult<IReadOnlyList<PendingAccountResponse>>> GetPendingVolunteers(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetPendingApplicationsAsync(
            cancellationToken, RoleNames.Volunteer));

    [HttpPost("volunteers/{userId:guid}/approve")]
    [Authorize(Roles = $"{RoleNames.VolunteerCoordinator},{RoleNames.Admin}")]
    public Task<IActionResult> ApproveVolunteer(
        Guid userId,
        CancellationToken cancellationToken) =>
        DecideAsync(userId, RoleNames.Volunteer, true, cancellationToken);

    [HttpPost("volunteers/{userId:guid}/reject")]
    [Authorize(Roles = $"{RoleNames.VolunteerCoordinator},{RoleNames.Admin}")]
    public Task<IActionResult> RejectVolunteer(
        Guid userId,
        CancellationToken cancellationToken) =>
        DecideAsync(userId, RoleNames.Volunteer, false, cancellationToken);

    private async Task<IActionResult> DecideAsync(
        Guid userId,
        string accountType,
        bool approve,
        CancellationToken cancellationToken)
    {
        var result = approve
            ? await _service.ApproveApplicationAsync(
                userId, cancellationToken, accountType)
            : await _service.RejectApplicationAsync(
                userId, cancellationToken, accountType);

        return result switch
        {
            ApplicantDecisionResult.Updated => NoContent(),
            ApplicantDecisionResult.NotFound => NotFound(),
            _ => Conflict()
        };
    }
}
