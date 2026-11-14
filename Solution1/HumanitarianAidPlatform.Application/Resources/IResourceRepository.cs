
using HumanitarianAidPlatform.Domain.Entities.Resources;

namespace HumanitarianAidPlatform.Application.Resources;

public interface IResourceRepository
{
    Task<List<Resource>> GetAllAsync();
    Task<Resource?> GetByIdAsync(Guid id);
    Task AddAsync(Resource resource);
    Task UpdateAsync(Resource resource);
    Task DeleteAsync(Resource resource);
}
