using HumanitarianAidPlatform.Domain.Entities.Volunteers;

namespace HumanitarianAidPlatform.Application.Volunteers;

public class VolunteerService : IVolunteerService
{
    private readonly IVolunteerRepository _repository;

    public VolunteerService(IVolunteerRepository repository)
    {
        _repository = repository;
    }

    public async Task<VolunteerResponseDto> CreateAsync(CreateVolunteerDto dto)
      
    {
        if (await _repository.EmailExistsAsync(dto.Email.Trim()))
            throw new InvalidOperationException(
                "A volunteer with this email already exists.");

        var volunteer = new Volunteer
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone?.Trim(),
            Skills = dto.Skills?.Trim(),
            Availability = dto.Availability,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(volunteer);
        return ToDto(volunteer);
    }

    public async Task<List<VolunteerResponseDto>> GetAllAsync()
    {
        var volunteers = await _repository.GetAllAsync();
        return volunteers.Select(ToDto).ToList();
    }

    public async Task<VolunteerResponseDto?> GetByIdAsync(Guid id)
    {
        var volunteer = await _repository.GetByIdAsync(id);
        return volunteer is null ? null : ToDto(volunteer);
    }

    public async Task<VolunteerResponseDto?> UpdateAsync(Guid id, CreateVolunteerDto dto)
    {
       
        var volunteer = await _repository.GetByIdAsync(id);
        if (volunteer is null) return null;

        if (await _repository.EmailExistsAsync(dto.Email.Trim(), id))
            throw new InvalidOperationException(
                "A volunteer with this email already exists.");

        volunteer.FullName = dto.FullName.Trim();
        volunteer.Email = dto.Email.Trim();
        volunteer.Phone = dto.Phone?.Trim();
        volunteer.Skills = dto.Skills?.Trim();
        volunteer.Availability = dto.Availability;

        await _repository.UpdateAsync(volunteer);
        return ToDto(volunteer);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var volunteer = await _repository.GetByIdAsync(id);
        if (volunteer is null) return false;

        await _repository.DeleteAsync(volunteer);
        return true;
    }

    private static VolunteerResponseDto ToDto(Volunteer v) =>
        new(v.Id, v.FullName, v.Email, v.Phone, v.Skills, v.Availability, v.CreatedAt);
}