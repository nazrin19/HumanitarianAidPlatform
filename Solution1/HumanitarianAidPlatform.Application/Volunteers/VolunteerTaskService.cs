using HumanitarianAidPlatform.Domain.Entities.Volunteers;

namespace HumanitarianAidPlatform.Application.Volunteers;

public class VolunteerTaskService : IVolunteerTaskService
{
    private const string Open = "Open";
    private const string Assigned = "Assigned";
    private const string Completed = "Completed";

    private readonly IVolunteerTaskRepository _tasks;
    private readonly IVolunteerRepository _volunteers;

    public VolunteerTaskService(IVolunteerTaskRepository tasks, IVolunteerRepository volunteers)
    {
        _tasks = tasks;
        _volunteers = volunteers;
    }

    public async Task<VolunteerTaskResponseDto> CreateAsync(CreateVolunteerTaskDto dto)
    {
        var task = new VolunteerTask
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Location = dto.Location?.Trim(),
            DueDate = dto.DueDate,
            Status = Open,
            CreatedAt = DateTime.UtcNow
        };

        await _tasks.AddAsync(task);
        return ToDto(task);
    }

    public async Task<List<VolunteerTaskResponseDto>> GetAllAsync(string? status, Guid? volunteerId)
    {
        var tasks = await _tasks.GetAllAsync(status, volunteerId);
        return tasks.Select(ToDto).ToList();
    }

    public async Task<VolunteerTaskResponseDto?> GetByIdAsync(Guid id)
    {
        var task = await _tasks.GetByIdAsync(id);
        return task is null ? null : ToDto(task);
    }

    public async Task<VolunteerTaskResponseDto?> AssignAsync(Guid taskId, Guid volunteerId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null) return null;

        if (task.Status == Completed)
            throw new InvalidOperationException("A completed task cannot be reassigned.");

        var volunteer = await _volunteers.GetByIdAsync(volunteerId)
            ?? throw new ArgumentException("Volunteer not found.");
        if (volunteer.Availability == "Inactive")
            throw new ArgumentException("Volunteer is inactive.");

        task.VolunteerId = volunteer.Id;
        task.Volunteer = volunteer;
        task.Status = Assigned;

        await _tasks.UpdateAsync(task);
        return ToDto(task);
    }

    public async Task<VolunteerTaskResponseDto?> CompleteAsync(Guid taskId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null) return null;

        if (task.Status != Assigned)
            throw new InvalidOperationException("Only an assigned task can be completed.");

        task.Status = Completed;

        await _tasks.UpdateAsync(task);
        return ToDto(task);
    }

    private static VolunteerTaskResponseDto ToDto(VolunteerTask t) =>
        new(t.Id, t.VolunteerId, t.Volunteer?.FullName, t.Title, t.Description,
            t.Location, t.Status, t.DueDate, t.CreatedAt);
}