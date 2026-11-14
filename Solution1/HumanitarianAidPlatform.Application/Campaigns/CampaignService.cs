using HumanitarianAidPlatform.Domain.Entities.Donors;

namespace HumanitarianAidPlatform.Application.Campaigns;

public class CampaignService : ICampaignService
{
    private readonly ICampaignRepository _repository;

    public CampaignService(ICampaignRepository repository)
    {
        _repository = repository;
    }

    public async Task<CampaignResponseDto> CreateAsync(CreateCampaignDto dto)
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            TargetAmount = dto.TargetAmount,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = true
        };

        await _repository.AddAsync(campaign);
        return ToDto(campaign);
    }

    public async Task<List<CampaignResponseDto>> GetAllAsync()
    {
        var campaigns = await _repository.GetAllAsync();
        return campaigns.Select(ToDto).ToList();
    }

    public async Task<CampaignResponseDto?> GetByIdAsync(Guid id)
    {
        var campaign = await _repository.GetByIdAsync(id);
        return campaign is null ? null : ToDto(campaign);
    }

    public async Task<CampaignResponseDto?> UpdateAsync(Guid id, UpdateCampaignDto dto)
    {
        var campaign = await _repository.GetByIdAsync(id);
        if (campaign is null) return null;

        campaign.Name = dto.Name.Trim();
        campaign.Description = dto.Description?.Trim();
        campaign.TargetAmount = dto.TargetAmount;
        campaign.StartDate = dto.StartDate;
        campaign.EndDate = dto.EndDate;
        campaign.IsActive = dto.IsActive;

        await _repository.UpdateAsync(campaign);
        return ToDto(campaign);
    }

    private static CampaignResponseDto ToDto(Campaign c) =>
        new(c.Id, c.Name, c.Description, c.TargetAmount, c.StartDate, c.EndDate, c.IsActive);
}