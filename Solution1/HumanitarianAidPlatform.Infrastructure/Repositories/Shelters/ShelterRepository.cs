
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    // Update occupancy safely without exceeding shelter capacity.
    public async Task<ShelterOccupancyUpdateResult> ChangeOccupancyAsync(
        Guid id,
        int change,
        CancellationToken cancellationToken = default)
    {
        if (change == 0 || change == int.MinValue)
        {
            return ShelterOccupancyUpdateResult.InvalidChange;
        }

        var query = _context.Shelters
            .Where(shelter => shelter.Id == id);

        if (change > 0)
        {
            // Only add people when sufficient capacity remains.
            query = query.Where(shelter =>
                shelter.CurrentOccupancy <= shelter.Capacity - change);
        }
        else
        {
            // Only remove people when enough occupants exist.
            query = query.Where(shelter =>
                shelter.CurrentOccupancy >= -change);
        }

        // Execute one atomic SQL UPDATE.
        var affectedRows = await query.ExecuteUpdateAsync(
            setters => setters.SetProperty(
                shelter => shelter.CurrentOccupancy,
                shelter => shelter.CurrentOccupancy + change),
            cancellationToken);

        if (affectedRows == 1)
        {
            return ShelterOccupancyUpdateResult.Updated;
        }

        // Determine why the update did not happen.
        var currentShelter = await _context.Shelters
            .AsNoTracking()
            .Where(shelter => shelter.Id == id)
            .Select(shelter => new
            {
                shelter.Capacity,
                shelter.CurrentOccupancy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (currentShelter is null)
        {
            return ShelterOccupancyUpdateResult.NotFound;
        }

        if (change > 0)
        {
            return ShelterOccupancyUpdateResult.CapacityExceeded;
        }

        return ShelterOccupancyUpdateResult.WouldBecomeNegative;
    }
}
