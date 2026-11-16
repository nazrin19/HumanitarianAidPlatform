using HumanitarianAidPlatform.Application.Donors;
using HumanitarianAidPlatform.Domain.Entities.Donors;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Donors;

public class DonorRepository : IDonorRepository
{
    private readonly AppDbContext _db;

    public DonorRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Donor donor)
    {
        _db.Donors.Add(donor);
        await _db.SaveChangesAsync();
    }

    public async Task<Donor?> GetByIdAsync(Guid id) =>
        await _db.Donors.FirstOrDefaultAsync(d => d.Id == id);

    public async Task<List<Donor>> GetAllAsync() =>
        await _db.Donors.AsNoTracking().OrderBy(d => d.FullName).ToListAsync();

    public async Task UpdateAsync(Donor donor)
    {
        _db.Donors.Update(donor);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Donor donor)
    {
        _db.Donors.Remove(donor);
        await _db.SaveChangesAsync();
    }
}