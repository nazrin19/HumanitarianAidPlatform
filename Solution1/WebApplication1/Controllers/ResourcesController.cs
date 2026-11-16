
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using HumanitarianAidPlatform.Application.Resources;
using HumanitarianAidPlatform.Application.Resources.DTOs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HumanitarianAidPlatform.Api.Controllers;

[ApiController]
[Route("api/resources")]
public class ResourcesController : ControllerBase
{
    private readonly ResourceService _service;

    public ResourcesController(ResourceService service)
    {
        _service = service;
    }

    // GET: api/resources (Public)
    [HttpGet]
    public async Task<ActionResult<List<ResourceDto>>> GetAll()
    {
        var resources = await _service.GetAllAsync();
        return Ok(resources);
    }

    // GET: api/resources/{id} (Public)
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResourceDto>> GetById(Guid id)
    {
        var resource = await _service.GetByIdAsync(id);

        if (resource is null)
            return NotFound();

        return Ok(resource);
    }

    // POST: api/resources (Admin, ResourceManager)
    [Authorize(Roles = "Admin,ResourceManager")]
    [HttpPost]
    public async Task<ActionResult<ResourceDto>> Create(
        [FromBody] ResourceDto dto)
    {
        var created = await _service.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    // PUT: api/resources/{id} (Admin, ResourceManager)
    [Authorize(Roles = "Admin,ResourceManager")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ResourceDto>> Update(
        Guid id,
        [FromBody] ResourceDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);

        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    // DELETE: api/resources/{id} (Admin, ResourceManager)
    [Authorize(Roles = "Admin,ResourceManager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
