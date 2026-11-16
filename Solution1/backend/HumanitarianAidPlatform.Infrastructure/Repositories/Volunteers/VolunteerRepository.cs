using HumanitarianAidPlatform.Application.Volunteers;
using HumanitarianAidPlatform.Domain.Entities.Volunteers;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Volunteers;

public class VolunteerRepository : IVolunteerRepository
{
    private readonly AppDbContext _db;

    public VolunteerRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Volunteer volunteer)
    {
        _db.Volunteers.Add(volunteer);
        await _db.SaveChangesAsync();
    }

    public async Task<Volunteer?> GetByIdAsync(Guid id) =>
        await _db.Volunteers.FirstOrDefaultAsync(v => v.Id == id);

    public async Task<List<Volunteer>> GetAllAsync() =>
        await _db.Volunteers.AsNoTracking().OrderBy(v => v.FullName).ToListAsync();

    public async Task UpdateAsync(Volunteer volunteer)
    {
        _db.Volunteers.Update(volunteer);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Volunteer volunteer)
    {
        _db.Volunteers.Remove(volunteer);
        await _db.SaveChangesAsync();
    }
}