using HumanitarianAidPlatform.Domain.Entities.Donors;

namespace HumanitarianAidPlatform.Application.Donors;

public class DonorService : IDonorService
{
    private readonly IDonorRepository _repository;

    public DonorService(IDonorRepository repository)
    {
        _repository = repository;
    }

    public async Task<DonorResponseDto> CreateAsync(CreateDonorDto dto)
    {
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone?.Trim(),
            Type = dto.Type,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(donor);
        return ToDto(donor);
    }

    public async Task<List<DonorResponseDto>> GetAllAsync()
    {
        var donors = await _repository.GetAllAsync();
        return donors.Select(ToDto).ToList();
    }

    public async Task<DonorResponseDto?> GetByIdAsync(Guid id)
    {
        var donor = await _repository.GetByIdAsync(id);
        return donor is null ? null : ToDto(donor);
    }

    public async Task<DonorResponseDto?> UpdateAsync(Guid id, UpdateDonorDto dto)
    {
        var donor = await _repository.GetByIdAsync(id);
        if (donor is null) return null;

        donor.FullName = dto.FullName.Trim();
        donor.Email = dto.Email.Trim();
        donor.Phone = dto.Phone?.Trim();
        donor.Type = dto.Type;
        donor.IsActive = dto.IsActive;

        await _repository.UpdateAsync(donor);
        return ToDto(donor);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var donor = await _repository.GetByIdAsync(id);
        if (donor is null) return false;

        await _repository.DeleteAsync(donor);
        return true;
    }

    private static DonorResponseDto ToDto(Donor d) =>
        new(d.Id, d.FullName, d.Email, d.Phone, d.Type, d.IsActive, d.CreatedAt);
}