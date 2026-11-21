using HumanitarianAidPlatform.Application.Campaigns;
using HumanitarianAidPlatform.Domain.Entities.Donors;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Campaigns;

public class CampaignRepository : ICampaignRepository
{
    private readonly AppDbContext _db;

    public CampaignRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Campaign campaign)
    {
        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();
    }

    public async Task<Campaign?> GetByIdAsync(Guid id) =>
        await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Campaign>> GetAllAsync() =>
        await _db.Campaigns.AsNoTracking().OrderByDescending(c => c.StartDate).ToListAsync();

    public async Task UpdateAsync(Campaign campaign)
    {
        _db.Campaigns.Update(campaign);
        await _db.SaveChangesAsync();
    }
}