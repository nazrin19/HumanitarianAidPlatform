using HumanitarianAidPlatform.Application.Campaigns;
using HumanitarianAidPlatform.Application.Donations;
using HumanitarianAidPlatform.Application.Donors;
using HumanitarianAidPlatform.Application.Volunteers;

namespace HumanitarianAidPlatform.Application.UserViews;

public class UserViewService : IUserViewService
{
    private readonly IDonorRepository _donors;
    private readonly IVolunteerRepository _volunteers;
    private readonly ICampaignService _campaigns;
    private readonly IDonationService _donations;
    private readonly IVolunteerTaskService _tasks;

    public UserViewService(
        IDonorRepository donors,
        IVolunteerRepository volunteers,
        ICampaignService campaigns,
        IDonationService donations,
        IVolunteerTaskService tasks)
    {
        _donors = donors;
        _volunteers = volunteers;
        _campaigns = campaigns;
        _donations = donations;
        _tasks = tasks;
    }

    public async Task<DonorUserViewDto?> GetDonorViewAsync(Guid userId)
    {
        var donor = await _donors.GetByUserIdAsync(userId);
        if (donor is null)
            return null;

        var campaigns = (await _campaigns.GetAllAsync())
            .Where(campaign => campaign.IsActive)
            .ToList();
        var donations = await _donations.GetAllAsync(donor.Id, null);

        return new DonorUserViewDto(
            donor.Id,
            donor.FullName,
            donor.Type,
            donor.IsActive,
            campaigns,
            donations);
    }

    public async Task<VolunteerUserViewDto?> GetVolunteerViewAsync(Guid userId)
    {
        var volunteer = await _volunteers.GetByUserIdAsync(userId);
        if (volunteer is null)
            return null;

        var tasks = await _tasks.GetAllAsync(null, volunteer.Id);

        return new VolunteerUserViewDto(
            volunteer.Id,
            volunteer.FullName,
            volunteer.Skills,
            volunteer.Availability,
            tasks);
    }
}
