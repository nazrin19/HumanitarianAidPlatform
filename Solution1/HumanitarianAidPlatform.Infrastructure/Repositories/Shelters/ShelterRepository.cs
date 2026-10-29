
using HumanitarianAidPlatform.Application.Shelters;
using HumanitarianAidPlatform.Domain.Entities.Shelters;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Shelters;

public class ShelterRepository : IShelterRepository
{
    private readonly AppDbContext _context;

    public ShelterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Shelter>> GetAllAsync()
    {
        return await _context.Shelters.ToListAsync();
    }

    public async Task<Shelter?> GetByIdAsync(Guid id)
    {
        return await _context.Shelters.FindAsync(id);
    }

    public async Task AddAsync(Shelter shelter)
    {
        await _context.Shelters.AddAsync(shelter);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Shelter shelter)
    {
        _context.Shelters.Update(shelter);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Shelter shelter)
    {
        _context.Shelters.Remove(shelter);
        await _context.SaveChangesAsync();
    }
}
