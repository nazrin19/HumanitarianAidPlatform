using HumanitarianAidPlatform.Application.Ai;
using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Domain.Entities.Users;

namespace HumanitarianAidPlatform.Application.Requests;

public class RequestService : IRequestService
{
    // Roles that may view any request. A citizen may only view their own.
    private static readonly HashSet<string> StaffRoles = new()
    {
        RoleNames.Coordinator,
        RoleNames.CaseManager,
        RoleNames.ResourceManager,
        RoleNames.ShelterAdmin,
        RoleNames.Admin
    };

    // Roles that may see AI recommendations (they review and decide on them).
    private static readonly HashSet<string> ReviewerRoles = new()
    {
        RoleNames.Coordinator,
        RoleNames.CaseManager,
        RoleNames.Admin
    };

    private readonly IRequestRepository _repository;
    private readonly IUrgencyClassifier _classifier;

    public RequestService(IRequestRepository repository, IUrgencyClassifier classifier)
    {
        _repository = repository;
        _classifier = classifier;
    }

    public async Task<RequestResponseDto> SubmitAsync(Guid citizenId, CreateRequestDto dto)
    {
        var now = DateTime.UtcNow;

        var request = new AssistanceRequest
        {
            Id = Guid.NewGuid(),
            CitizenId = citizenId,
            Category = dto.Category!.Value,
            Description = dto.Description.Trim(),
            Status = RequestStatus.Submitted,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address?.Trim(),
            CreatedAt = now
        };

        request.StatusHistory.Add(new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            AssistanceRequestId = request.Id,
            FromStatus = null,
            ToStatus = RequestStatus.Submitted,
            ChangedByUserId = citizenId,
            ChangedAt = now,
            Note = "Request submitted"
        });

        // The request is saved first, so it is never lost even if classification fails.
        await _repository.AddAsync(request);

        try
        {
            var result = await _classifier.ClassifyAsync(request);
            var classifiedAt = DateTime.UtcNow;

            var recommendation = new AiRecommendation
            {
                Id = Guid.NewGuid(),
                AssistanceRequestId = request.Id,
                SuggestedCategory = result.SuggestedCategory,
                UrgencyScore = result.UrgencyScore,
                UrgencyLevel = result.UrgencyLevel,
                Confidence = result.Confidence,
                Rationale = result.Rationale,
                ModelName = _classifier.Name,
                GeneratedAt = classifiedAt
            };

            var history = new RequestStatusHistory
            {
                Id = Guid.NewGuid(),
                AssistanceRequestId = request.Id,
                FromStatus = RequestStatus.Submitted,
                ToStatus = RequestStatus.PendingReview,
                ChangedByUserId = null, // changed by the system, not by a person
                ChangedAt = classifiedAt,
                Note = $"AI recommendation ready ({_classifier.Name})"
            };

            await _repository.AttachRecommendationAsync(request.Id, recommendation, history);
            request.Status = RequestStatus.PendingReview;
        }
        catch (Exception)
        {
            // TODO (task A4): log this and mark the request "needs manual review".
            // For now the request stays "Submitted" and a person picks it up.
        }

        return ToDto(request);
    }

    public async Task<RequestResponseDto?> GetByIdAsync(Guid id, Guid requesterId, string requesterRole)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request is null || !CanView(request, requesterId, requesterRole))
            return null;

        return ToDto(request);
    }

    public async Task<IReadOnlyList<RequestStatusHistoryDto>?> GetHistoryAsync(Guid id, Guid requesterId, string requesterRole)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request is null || !CanView(request, requesterId, requesterRole))
            return null;

        var history = await _repository.GetHistoryAsync(id);
        return history
            .Select(h => new RequestStatusHistoryDto(h.FromStatus, h.ToStatus, h.ChangedByUserId, h.ChangedAt, h.Note))
            .ToList();
    }

    public async Task<RecommendationDto?> GetRecommendationAsync(Guid id, string requesterRole)
    {
        if (!ReviewerRoles.Contains(requesterRole))
            return null;

        var rec = await _repository.GetRecommendationAsync(id);
        return rec is null
            ? null
            : new RecommendationDto(rec.SuggestedCategory, rec.UrgencyScore, rec.UrgencyLevel,
                                    rec.Confidence, rec.Rationale, rec.ModelName, rec.GeneratedAt);
    }

    // Owner or staff only. Everyone else gets "not found" so they can't tell the request exists.
    private static bool CanView(AssistanceRequest request, Guid requesterId, string requesterRole) =>
        request.CitizenId == requesterId || StaffRoles.Contains(requesterRole);

    private static RequestResponseDto ToDto(AssistanceRequest r) =>
        new(r.Id, r.CitizenId, r.Category, r.Description, r.Status, r.Latitude, r.Longitude, r.Address, r.CreatedAt);
}