using HumanitarianAidPlatform.Application.Campaigns;
using HumanitarianAidPlatform.Application.Donors;
using HumanitarianAidPlatform.Domain.Entities.Donors;

namespace HumanitarianAidPlatform.Application.Donations;

public class DonationService : IDonationService
{
    private readonly IDonationRepository _donations;
    private readonly IDonorRepository _donors;
    private readonly ICampaignRepository _campaigns;

    public DonationService(
        IDonationRepository donations,
        IDonorRepository donors,
        ICampaignRepository campaigns)
    {
        _donations = donations;
        _donors = donors;
        _campaigns = campaigns;
    }

    public async Task<DonationResponseDto> CreateAsync(CreateDonationDto dto)
    {
        var donor = await _donors.GetByIdAsync(dto.DonorId)
            ?? throw new ArgumentException("Donor not found.");
        if (!donor.IsActive)
            throw new ArgumentException("Donor is inactive.");

        Campaign? campaign = null;
        if (dto.CampaignId is Guid campaignId)
        {
            campaign = await _campaigns.GetByIdAsync(campaignId)
                ?? throw new ArgumentException("Campaign not found.");
            if (!campaign.IsActive)
                throw new ArgumentException("Campaign is closed.");
        }

        var donation = new Donation
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            CampaignId = campaign?.Id,
            Type = dto.Type,
            Amount = dto.Amount,
            Currency = dto.Currency.ToUpperInvariant(),
            Description = dto.Description?.Trim(),
            DonatedAt = DateTime.UtcNow
        };

        await _donations.AddAsync(donation);
        return ToDto(donation, donor.FullName, campaign?.Name);
    }

    public async Task<List<DonationResponseDto>> GetAllAsync(Guid? donorId, Guid? campaignId)
    {
        var donations = await _donations.GetAllAsync(donorId, campaignId);
        return donations.Select(d => ToDto(d, d.Donor.FullName, d.Campaign?.Name)).ToList();
    }

    public async Task<DonationResponseDto?> GetByIdAsync(Guid id)
    {
        var d = await _donations.GetByIdAsync(id);
        return d is null ? null : ToDto(d, d.Donor.FullName, d.Campaign?.Name);
    }

    private static DonationResponseDto ToDto(Donation d, string donorName, string? campaignName) =>
        new(d.Id, d.DonorId, donorName, d.CampaignId, campaignName,
            d.Type, d.Amount, d.Currency, d.Description, d.DonatedAt);
}