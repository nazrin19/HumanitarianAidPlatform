
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using HumanitarianAidPlatform.Application.Resources.DTOs;
using HumanitarianAidPlatform.Domain.Entities.Resources;

namespace HumanitarianAidPlatform.Application.Resources;

public class ResourceService
{
    private readonly IResourceRepository _repository;

    public ResourceService(IResourceRepository repository)
    {
        _repository = repository;
    }

    // GET ALL
    public async Task<List<ResourceDto>> GetAllAsync()
    {
        var resources = await _repository.GetAllAsync();

        return resources.Select(MapToDto).ToList();
    }

    // GET BY ID
    public async Task<ResourceDto?> GetByIdAsync(Guid id)
    {
        var resource = await _repository.GetByIdAsync(id);

        return resource is null ? null : MapToDto(resource);
    }

    // CREATE
    public async Task<ResourceDto> CreateAsync(ResourceDto dto)
    {
        ValidateCoordinates(dto);
        if (dto.QuantityAvailable < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dto.QuantityAvailable),
                dto.QuantityAvailable,
                "Quantity cannot be negative.");
        }

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Category = dto.Category.Trim(),
            QuantityAvailable = dto.QuantityAvailable,
            Description = dto.Description?.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

        await _repository.AddAsync(resource);

        return MapToDto(resource);
    }

    // UPDATE
    public async Task<ResourceDto?> UpdateAsync(
        Guid id,
        ResourceDto dto)
    {
        ValidateCoordinates(dto);
        if (dto.QuantityAvailable < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dto.QuantityAvailable),
                dto.QuantityAvailable,
                "Quantity cannot be negative.");
        }

        var resource = await _repository.GetByIdAsync(id);

        if (resource is null)
        {
            return null;
        }

        resource.Name = dto.Name.Trim();
        resource.Category = dto.Category.Trim();
        resource.QuantityAvailable = dto.QuantityAvailable;
        resource.Description = dto.Description?.Trim();
        resource.Latitude = dto.Latitude;
        resource.Longitude = dto.Longitude;

        await _repository.UpdateAsync(resource);

        return MapToDto(resource);
    }

    // DELETE
    public async Task<bool> DeleteAsync(Guid id)
    {
        var resource = await _repository.GetByIdAsync(id);

        if (resource is null)
        {
            return false;
        }

        await _repository.DeleteAsync(resource);

        return true;
    }

    // DTO MAPPING
    private static ResourceDto MapToDto(Resource resource)
    {
        return new ResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Category = resource.Category,
            QuantityAvailable = resource.QuantityAvailable,
            Description = resource.Description,
            Latitude = resource.Latitude,
            Longitude = resource.Longitude
        };
    }

    private static void ValidateCoordinates(ResourceDto dto)
    {
        if (dto.Latitude.HasValue != dto.Longitude.HasValue)
        {
            throw new ArgumentException(
                "Latitude and longitude must be provided together.");
        }
    }
}
