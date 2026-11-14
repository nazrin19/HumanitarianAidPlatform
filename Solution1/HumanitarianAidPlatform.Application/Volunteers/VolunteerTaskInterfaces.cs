using HumanitarianAidPlatform.Domain.Entities.Volunteers;

namespace HumanitarianAidPlatform.Application.Volunteers;

public interface IVolunteerTaskRepository
{
    Task AddAsync(VolunteerTask task);
    Task<VolunteerTask?> GetByIdAsync(Guid id);
    Task<List<VolunteerTask>> GetAllAsync(string? status, Guid? volunteerId);
    Task UpdateAsync(VolunteerTask task);
}

public interface IVolunteerTaskService
{
    Task<VolunteerTaskResponseDto> CreateAsync(CreateVolunteerTaskDto dto);
    Task<List<VolunteerTaskResponseDto>> GetAllAsync(string? status, Guid? volunteerId);
    Task<VolunteerTaskResponseDto?> GetByIdAsync(Guid id);

    /// <exception cref="ArgumentException">Volunteer not found or inactive.</exception>
    /// <exception cref="InvalidOperationException">Task is already completed.</exception>
    Task<VolunteerTaskResponseDto?> AssignAsync(Guid taskId, Guid volunteerId);

    /// <exception cref="InvalidOperationException">Task is not assigned yet.</exception>
    Task<VolunteerTaskResponseDto?> CompleteAsync(Guid taskId);
}