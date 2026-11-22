using HumanitarianAidPlatform.Domain.Entities.Donors;

namespace HumanitarianAidPlatform.Application.Donations;

public interface IDonationRepository
{
    Task AddAsync(Donation donation);
    Task<Donation?> GetByIdAsync(Guid id);
    Task<List<Donation>> GetAllAsync(Guid? donorId, Guid? campaignId);
}

public interface IDonationService
{
    /// <exception cref="ArgumentException">Donor or campaign is missing or inactive.</exception>
    Task<DonationResponseDto> CreateAsync(CreateDonationDto dto);
    Task<List<DonationResponseDto>> GetAllAsync(Guid? donorId, Guid? campaignId);
    Task<DonationResponseDto?> GetByIdAsync(Guid id);
}