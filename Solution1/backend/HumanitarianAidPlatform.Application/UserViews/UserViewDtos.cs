using HumanitarianAidPlatform.Application.Campaigns;
using HumanitarianAidPlatform.Application.Donations;
using HumanitarianAidPlatform.Application.Volunteers;

namespace HumanitarianAidPlatform.Application.UserViews;

public record DonorUserViewDto(
    Guid Id,
    string FullName,
    string Type,
    bool IsActive,
    IReadOnlyList<CampaignResponseDto> Campaigns,
    IReadOnlyList<DonationResponseDto> Donations);

public record VolunteerUserViewDto(
    Guid Id,
    string FullName,
    string? Skills,
    string Availability,
    IReadOnlyList<VolunteerTaskResponseDto> Tasks);
