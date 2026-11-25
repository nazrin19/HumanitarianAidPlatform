using HumanitarianAidPlatform.Application.Donors;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/donors")]
[Authorize(Roles = $"{RoleNames.DonorOfficer},{RoleNames.Admin}")]
public class DonorsController : ControllerBase
{
    private readonly IDonorService _service;

    public DonorsController(IDonorService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DonorResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    public async Task<ActionResult<DonorResponseDto>> Create(CreateDonorDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }


    [HttpGet]
    public async Task<ActionResult<List<DonorResponseDto>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DonorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DonorResponseDto>> GetById(Guid id)
    {
        var donor = await _service.GetByIdAsync(id);
        return donor is null ? NotFound() : Ok(donor);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DonorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    public async Task<ActionResult<DonorResponseDto>> Update(Guid id, UpdateDonorDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            return await _service.DeleteAsync(id)
                ? NoContent()
                : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

}