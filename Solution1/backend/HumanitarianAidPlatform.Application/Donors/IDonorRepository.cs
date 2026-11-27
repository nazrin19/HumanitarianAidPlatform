using HumanitarianAidPlatform.Domain.Entities.Donors;

namespace HumanitarianAidPlatform.Application.Donors;

public interface IDonorRepository
{
    Task AddAsync(Donor donor);
    Task<Donor?> GetByIdAsync(Guid id);
    Task<Donor?> GetByUserIdAsync(Guid userId);
    Task<List<Donor>> GetAllAsync();
    Task UpdateAsync(Donor donor);
    Task DeleteAsync(Donor donor);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
    Task<bool> HasDonationsAsync(Guid donorId);
}