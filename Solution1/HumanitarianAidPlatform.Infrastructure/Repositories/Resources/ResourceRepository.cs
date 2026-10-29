
using HumanitarianAidPlatform.Application.Resources;
using HumanitarianAidPlatform.Domain.Entities.Resources;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Resources;

public class ResourceRepository : IResourceRepository
{
    private readonly AppDbContext _context;

    public ResourceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Resource>> GetAllAsync()
    {
        return await _context.Resources.ToListAsync();
    }

    public async Task<Resource?> GetByIdAsync(Guid id)
    {
        return await _context.Resources.FindAsync(id);
    }

    public async Task AddAsync(Resource resource)
    {
        await _context.Resources.AddAsync(resource);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Resource resource)
    {
        _context.Resources.Update(resource);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Resource resource)
    {
        _context.Resources.Remove(resource);
        await _context.SaveChangesAsync();
    }
}

