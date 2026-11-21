using HumanitarianAidPlatform.Domain.Entities.Donors;

namespace HumanitarianAidPlatform.Application.Campaigns;

public interface ICampaignRepository
{
    Task AddAsync(Campaign campaign);
    Task<Campaign?> GetByIdAsync(Guid id);
    Task<List<Campaign>> GetAllAsync();
    Task UpdateAsync(Campaign campaign);
}

public interface ICampaignService
{
    Task<CampaignResponseDto> CreateAsync(CreateCampaignDto dto);
    Task<List<CampaignResponseDto>> GetAllAsync();
    Task<CampaignResponseDto?> GetByIdAsync(Guid id);
    Task<CampaignResponseDto?> UpdateAsync(Guid id, UpdateCampaignDto dto);
}