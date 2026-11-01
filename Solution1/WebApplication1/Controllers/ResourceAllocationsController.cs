
using System;
using System.Threading;
using System.Threading.Tasks;

using HumanitarianAidPlatform.Application.Resources;
using HumanitarianAidPlatform.Application.Resources.DTOs;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/resource-allocations")]
public class ResourceAllocationsController : ControllerBase
{
    private readonly ResourceAllocationService _service;

    public ResourceAllocationsController(
        ResourceAllocationService service)
    {
        _service = service;
    }

    // POST: api/resource-allocations
    [HttpPost]
    public async Task<ActionResult<ResourceAllocationDto>> Create(
        [FromBody] CreateResourceAllocationDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.AllocateAsync(
            dto,
            cancellationToken);

        if (result.Allocation is not null)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                result.Allocation);
        }

        switch (result.Failure)
        {
            case ResourceAllocationFailure.InvalidResourceId:
                return BadRequest(new
                {
                    message = "A valid resource ID is required."
                });

            case ResourceAllocationFailure.InvalidAssistanceRequestId:
                return BadRequest(new
                {
                    message = "A valid assistance request ID is required."
                });

            case ResourceAllocationFailure.InvalidQuantity:
                return BadRequest(new
                {
                    message = "Allocation quantity must be greater than zero."
                });

            case ResourceAllocationFailure.ResourceNotFound:
                return NotFound(new
                {
                    message = "Resource not found."
                });

            case ResourceAllocationFailure.AssistanceRequestNotFound:
                return NotFound(new
                {
                    message = "Assistance request not found."
                });

            case ResourceAllocationFailure.InsufficientQuantity:
                return Conflict(new
                {
                    message = "Insufficient resource quantity available."
                });

            default:
                return StatusCode(
                    StatusCodes.Status500InternalServerError);
        }
    }
}
