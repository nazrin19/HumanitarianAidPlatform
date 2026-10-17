using HumanitarianAidPlatform.Application.Requests;
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
    [ProducesResponseType(typeof(RequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequestResponseDto>> Submit(CreateRequestDto dto)
    {
        var created = await _service.SubmitAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestResponseDto>> GetById(Guid id)
    {
        var request = await _service.GetByIdAsync(id);
        return request is null ? NotFound() : Ok(request);
    }
}
