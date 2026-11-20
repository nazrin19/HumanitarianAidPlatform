using HumanitarianAidPlatform.Application.Donations;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/donations")]
[Authorize(Roles = $"{RoleNames.DonorOfficer},{RoleNames.Admin}")]
public class DonationsController : ControllerBase
{
    private readonly IDonationService _service;

    public DonationsController(IDonationService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DonationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DonationResponseDto>> Create(CreateDonationDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<DonationResponseDto>>> GetAll(
        [FromQuery] Guid? donorId, [FromQuery] Guid? campaignId) =>
        Ok(await _service.GetAllAsync(donorId, campaignId));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DonationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DonationResponseDto>> GetById(Guid id)
    {
        var donation = await _service.GetByIdAsync(id);
        return donation is null ? NotFound() : Ok(donation);
    }
}