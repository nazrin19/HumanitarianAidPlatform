
using HumanitarianAidPlatform.Domain.Entities.Shelters;

namespace HumanitarianAidPlatform.Application.Shelters;

public interface IShelterRepository
{
    Task<List<Shelter>> GetAllAsync();
    Task<Shelter?> GetByIdAsync(Guid id);
    Task AddAsync(Shelter shelter);
    Task UpdateAsync(Shelter shelter);
    Task DeleteAsync(Shelter shelter);
}

