namespace HumanitarianAidPlatform.Application.Donors;

public interface IDonorService
{
    Task<DonorResponseDto> CreateAsync(CreateDonorDto dto);
    Task<List<DonorResponseDto>> GetAllAsync();
    Task<DonorResponseDto?> GetByIdAsync(Guid id);
    Task<DonorResponseDto?> UpdateAsync(Guid id, UpdateDonorDto dto);
    Task<bool> DeleteAsync(Guid id);
}