using HumanitarianAidPlatform.Application.Requests;
using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Requests;

public class RequestRepository : IRequestRepository
{
    private readonly AppDbContext _db;

    public RequestRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AssistanceRequest request)
    {
        _db.AssistanceRequests.Add(request); // also saves the history entries attached to it
        await _db.SaveChangesAsync();
    }

    public async Task<AssistanceRequest?> GetByIdAsync(Guid id) =>
        await _db.AssistanceRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(Guid requestId) =>
        await _db.RequestStatusHistories
            .AsNoTracking()
            .Where(h => h.AssistanceRequestId == requestId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

    // Recommendation, status change and history entry are saved together in one transaction.
    public async Task AttachRecommendationAsync(Guid requestId, AiRecommendation recommendation, RequestStatusHistory history)
    {
        var request = await _db.AssistanceRequests.FirstAsync(r => r.Id == requestId);
        request.Status = history.ToStatus;
        _db.AiRecommendations.Add(recommendation);
        _db.RequestStatusHistories.Add(history);
        await _db.SaveChangesAsync();
    }

    public async Task<AiRecommendation?> GetRecommendationAsync(Guid requestId) =>
        await _db.AiRecommendations.AsNoTracking().FirstOrDefaultAsync(a => a.AssistanceRequestId == requestId);
}