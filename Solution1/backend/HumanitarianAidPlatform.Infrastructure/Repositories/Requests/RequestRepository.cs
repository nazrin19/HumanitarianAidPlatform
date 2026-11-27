using HumanitarianAidPlatform.Application.Requests;
using HumanitarianAidPlatform.Application.Common;
using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Requests;

public class RequestRepository : IRequestRepository
{
    private static readonly TimeSpan OverdueAfter = TimeSpan.FromHours(24);
    private readonly AppDbContext _db;
    private readonly ILogger<RequestRepository> _logger;

    public RequestRepository(
        AppDbContext db,
        ILogger<RequestRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AddAsync(AssistanceRequest request)
    {
        _db.AssistanceRequests.Add(request); // also saves the history entries attached to it
        await _db.SaveChangesAsync();
    }

    public async Task<AssistanceRequest?> GetByIdAsync(Guid id) =>
        await _db.AssistanceRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    public async Task<AssistanceRequest?> GetTrackedByIdAsync(Guid id) =>
        await _db.AssistanceRequests.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<string?> GetNotificationRecipientAsync(Guid requestId)
    {
        var request = await _db.AssistanceRequests
            .AsNoTracking()
            .Where(item => item.Id == requestId)
            .Select(item => new { item.CitizenId })
            .FirstOrDefaultAsync();

        if (request is null)
        {
            _logger.LogWarning(
                "Could not resolve notification recipient for request {RequestId}: request row was not found.",
                requestId);
            return null;
        }

        var user = await _db.Users
            .AsNoTracking()
            .Where(item => item.Id == request.CitizenId)
            .Select(item => new { item.Email })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            _logger.LogWarning(
                "Could not resolve notification recipient for request {RequestId}: citizen join failed for user {UserId}.",
                requestId,
                request.CitizenId);
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning(
                "Could not resolve notification recipient for request {RequestId}: user {UserId} has no email address.",
                requestId,
                request.CitizenId);
            return null;
        }

        return user.Email;
    }

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

    public async Task SaveReviewAsync(
        RequestStatusHistory history,
        AuditLog auditLog)
    {
        var request = await _db.AssistanceRequests.FirstAsync(r => r.Id == history.AssistanceRequestId);
        request.Status = history.ToStatus;
        _db.RequestStatusHistories.Add(history);
        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync();
    }

    public async Task SaveStatusChangeAsync(RequestStatusHistory history)
    {
        var request = await _db.AssistanceRequests.FirstAsync(r => r.Id == history.AssistanceRequestId);
        request.Status = history.ToStatus;
        _db.RequestStatusHistories.Add(history);
        await _db.SaveChangesAsync();
    }

    public async Task<AiRecommendation?> GetRecommendationAsync(Guid requestId) =>
        await _db.AiRecommendations.AsNoTracking().FirstOrDefaultAsync(a => a.AssistanceRequestId == requestId);

    public Task<PagedResult<RequestListItemDto>> GetMineAsync(
        Guid citizenId,
        RequestListQuery query) =>
        GetPagedAsync(_db.AssistanceRequests.Where(r => r.CitizenId == citizenId), query);

    public Task<PagedResult<RequestListItemDto>> GetForStaffAsync(
        RequestListQuery query) =>
        GetPagedAsync(_db.AssistanceRequests, query);

    private async Task<PagedResult<RequestListItemDto>> GetPagedAsync(
        IQueryable<AssistanceRequest> source,
        RequestListQuery query)
    {
        if (query.Status.HasValue)
            source = source.Where(r => r.Status == query.Status.Value);

        if (query.Category.HasValue)
            source = source.Where(r => r.Category == query.Category.Value);

        var recommendations = _db.AiRecommendations.AsNoTracking();
        if (query.Urgency.HasValue)
        {
            source = source.Where(r => recommendations.Any(a =>
                a.AssistanceRequestId == r.Id &&
                a.UrgencyLevel == query.Urgency.Value));
        }

        if (query.Overdue.HasValue)
        {
            var cutoff = DateTime.UtcNow.Subtract(OverdueAfter);
            var overdue = source.Where(r =>
                r.Status == RequestStatus.Submitted ||
                r.Status == RequestStatus.PendingReview);

            overdue = overdue.Where(r => r.CreatedAt <= cutoff);
            source = query.Overdue.Value
                ? overdue
                : source.Where(r => !(
                    (r.Status == RequestStatus.Submitted ||
                     r.Status == RequestStatus.PendingReview) &&
                    r.CreatedAt <= cutoff));
        }

        var totalCount = await source.CountAsync();
        var items = await source
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .GroupJoin(
                recommendations,
                request => request.Id,
                recommendation => recommendation.AssistanceRequestId,
                (request, requestRecommendations) => new
                {
                    Request = request,
                    Recommendation = requestRecommendations.FirstOrDefault()
                })
            .Select(x => new RequestListItemDto(
                x.Request.Id,
                x.Request.CitizenId,
                x.Request.Category,
                x.Request.Description,
                x.Request.Status,
                x.Request.Latitude,
                x.Request.Longitude,
                x.Request.Address,
                x.Request.CreatedAt,
                x.Recommendation == null ? null : x.Recommendation.UrgencyLevel,
                x.Recommendation == null ? null : x.Recommendation.UrgencyScore,
                x.Recommendation == null ? null : x.Recommendation.Confidence))
            .ToListAsync();

        return new PagedResult<RequestListItemDto>(
            items,
            query.Page,
            query.PageSize,
            totalCount);
    }
}