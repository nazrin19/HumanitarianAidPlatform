
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

    public async Task<List<ResourceDto>> GetAllAsync()
    {
        var resources = await _repository.GetAllAsync();

        return resources.Select(r => new ResourceDto
        {
            Id = r.Id,
            Name = r.Name,
            Category = r.Category,
            QuantityAvailable = r.QuantityAvailable,
            Description = r.Description
        }).ToList();
    }
}
