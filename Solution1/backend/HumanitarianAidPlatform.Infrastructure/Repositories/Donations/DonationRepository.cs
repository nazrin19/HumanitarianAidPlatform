using HumanitarianAidPlatform.Application.Donations;
using HumanitarianAidPlatform.Domain.Entities.Donors;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Donations;

public class DonationRepository : IDonationRepository
{
    private readonly AppDbContext _db;

    public DonationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Donation donation)
    {
        _db.Donations.Add(donation);
        await _db.SaveChangesAsync();
    }

    public async Task<Donation?> GetByIdAsync(Guid id) =>
        await _db.Donations
            .Include(d => d.Donor)
            .Include(d => d.Campaign)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<List<Donation>> GetAllAsync(Guid? donorId, Guid? campaignId)
    {
        var query = _db.Donations
            .Include(d => d.Donor)
            .Include(d => d.Campaign)
            .AsNoTracking()
            .AsQueryable();

        if (donorId.HasValue) query = query.Where(d => d.DonorId == donorId);
        if (campaignId.HasValue) query = query.Where(d => d.CampaignId == campaignId);

        return await query.OrderByDescending(d => d.DonatedAt).ToListAsync();
    }
}