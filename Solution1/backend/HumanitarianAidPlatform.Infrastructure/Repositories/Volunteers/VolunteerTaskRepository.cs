using HumanitarianAidPlatform.Application.Volunteers;
using HumanitarianAidPlatform.Domain.Entities.Volunteers;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Volunteers;

public class VolunteerTaskRepository : IVolunteerTaskRepository
{
    private readonly AppDbContext _db;

    public VolunteerTaskRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(VolunteerTask task)
    {
        _db.VolunteerTasks.Add(task);
        await _db.SaveChangesAsync();
    }

    public async Task<VolunteerTask?> GetByIdAsync(Guid id) =>
        await _db.VolunteerTasks
            .Include(t => t.Volunteer)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<VolunteerTask>> GetAllAsync(string? status, Guid? volunteerId)
    {
        var query = _db.VolunteerTasks
            .Include(t => t.Volunteer)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(t => t.Status == status);
        if (volunteerId.HasValue) query = query.Where(t => t.VolunteerId == volunteerId);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task UpdateAsync(VolunteerTask task)
    {
        await _db.SaveChangesAsync();
    }
}