using HumanitarianAidPlatform.Domain.Entities.Volunteers;

namespace HumanitarianAidPlatform.Application.Volunteers;

public interface IVolunteerRepository
{
    Task AddAsync(Volunteer volunteer);
    Task<Volunteer?> GetByIdAsync(Guid id);
    Task<List<Volunteer>> GetAllAsync();
    Task UpdateAsync(Volunteer volunteer);
    Task DeleteAsync(Volunteer volunteer);
}