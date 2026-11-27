namespace HumanitarianAidPlatform.Application.UserViews;

public interface IUserViewService
{
    Task<DonorUserViewDto?> GetDonorViewAsync(Guid userId);
    Task<VolunteerUserViewDto?> GetVolunteerViewAsync(Guid userId);
}
