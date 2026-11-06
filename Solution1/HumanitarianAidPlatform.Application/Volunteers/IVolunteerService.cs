namespace HumanitarianAidPlatform.Application.Volunteers;

public interface IVolunteerService
{
    Task<VolunteerResponseDto> CreateAsync(CreateVolunteerDto dto);
    Task<List<VolunteerResponseDto>> GetAllAsync();
    Task<VolunteerResponseDto?> GetByIdAsync(Guid id);
    Task<VolunteerResponseDto?> UpdateAsync(Guid id, CreateVolunteerDto dto);
    Task<bool> DeleteAsync(Guid id);
}