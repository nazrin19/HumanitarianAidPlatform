using HumanitarianAidPlatform.Application.Ai;
using HumanitarianAidPlatform.Application.Common;
using HumanitarianAidPlatform.Application.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

    private static readonly HashSet<string> WorkflowRoles = new()
    {
        RoleNames.Coordinator,
        RoleNames.CaseManager,
        RoleNames.ResourceManager,
        RoleNames.Admin
    };

    private readonly IRequestRepository _repository;
    private readonly IUrgencyClassifier _classifier;
    private readonly INotificationService? _notificationService;
    private readonly ILogger<RequestService> _logger;

    public RequestService(
        IRequestRepository repository,
        IUrgencyClassifier classifier,
        ILogger<RequestService> logger)
        : this(repository, classifier, null, logger)
    {
    }

    public RequestService(
        IRequestRepository repository,
        IUrgencyClassifier classifier,
        INotificationService? notificationService,
        ILogger<RequestService> logger)
    {
        _repository = repository;
        _classifier = classifier;
        _notificationService = notificationService;
        _logger = logger;
    }

    public Task<PagedResult<RequestListItemDto>> GetMineAsync(
        Guid citizenId,
        RequestListQuery query) =>
        _repository.GetMineAsync(citizenId, Normalize(query));

    public Task<PagedResult<RequestListItemDto>> GetForStaffAsync(
        RequestListQuery query) =>
        _repository.GetForStaffAsync(Normalize(query));

    private static RequestListQuery Normalize(RequestListQuery query) =>
        new()
        {
            Status = query.Status,
            Urgency = query.Urgency,
            Category = query.Category,
            Overdue = query.Overdue,
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 100)
        };

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

        var submittedHistory = new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            AssistanceRequestId = request.Id,
            FromStatus = null,
            ToStatus = RequestStatus.Submitted,
            ChangedByUserId = citizenId,
            ChangedAt = now,
            Note = "Request submitted"
        };
        request.StatusHistory.Add(submittedHistory);
        var notificationEventId = submittedHistory.Id;

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
            notificationEventId = history.Id;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Urgency classification failed for request {RequestId} using {ClassifierName}. " +
                "The saved request remains available for manual review.",
                request.Id,
                _classifier.Name);
        }

        await SendStatusNotificationSafelyAsync(
            request.Id,
            notificationEventId,
            request.Status,
            "Request submitted");

        return ToDto(request);
    }

    public async Task<RequestResponseDto?> GetByIdAsync(Guid id, Guid requesterId, string requesterRole)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request is null || !CanView(request, requesterId, requesterRole))
            return null;

        return ToDto(request);
    }

    public async Task<ReviewResult> ReviewAsync(
        Guid id,
        Guid reviewerId,
        string reviewerRole,
        ReviewRequestDto dto)
    {
        if (!ReviewerRoles.Contains(reviewerRole))
            return new ReviewResult(ReviewResultCode.CriticalCoordinatorRequired);

        var request = await _repository.GetTrackedByIdAsync(id);
        if (request is null)
            return new ReviewResult(ReviewResultCode.NotFound);

        var targetStatus = dto.Action switch
        {
            ReviewAction.Approved or ReviewAction.Modified => RequestStatus.Approved,
            ReviewAction.Rejected => RequestStatus.Rejected,
            ReviewAction.Deferred => RequestStatus.Deferred,
            _ => request.Status
        };

        if (!RequestStatusTransitions.CanTransition(request.Status, targetStatus))
            return new ReviewResult(ReviewResultCode.InvalidTransition);

        if ((dto.Action is ReviewAction.Rejected or ReviewAction.Deferred) &&
            string.IsNullOrWhiteSpace(dto.Reason))
            return new ReviewResult(ReviewResultCode.ReasonRequired);

        var originalValues = new
        {
            request.Category,
            request.Description
        };

        if (dto.Action == ReviewAction.Modified)
        {
            if (dto.Category.HasValue)
                request.Category = dto.Category.Value;
            if (dto.Description is not null)
                request.Description = dto.Description.Trim();
        }

        var changedValues = new
        {
            Category = dto.Category,
            Description = dto.Description
        };

        var recommendation = await _repository.GetRecommendationAsync(id);
        var now = DateTime.UtcNow;
        var history = new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            AssistanceRequestId = id,
            FromStatus = request.Status,
            ToStatus = targetStatus,
            ChangedByUserId = reviewerId,
            ChangedAt = now,
            Note = dto.Reason?.Trim()
        };
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            AssistanceRequestId = id,
            AiSnapshotJson = JsonSerializer.Serialize(recommendation),
            AiConfidence = recommendation?.Confidence,
            Action = dto.Action,
            ChangedValuesJson = dto.Action == ReviewAction.Modified
                ? JsonSerializer.Serialize(new { Original = originalValues, Changed = changedValues })
                : null,
            Reason = dto.Reason?.Trim(),
            DecidedByUserId = reviewerId,
            DecidedAt = now
        };

        await _repository.SaveReviewAsync(history, auditLog);
        await SendStatusNotificationSafelyAsync(
            id,
            history.Id,
            targetStatus,
            dto.Reason);
        return new ReviewResult(ReviewResultCode.Success, ToDto(request));
    }

    public async Task<ReviewResult> ChangeStatusAsync(
        Guid id,
        Guid userId,
        string userRole,
        ChangeRequestStatusDto dto)
    {
        if (!WorkflowRoles.Contains(userRole))
            return new ReviewResult(ReviewResultCode.CriticalCoordinatorRequired);

        var request = await _repository.GetTrackedByIdAsync(id);
        if (request is null)
            return new ReviewResult(ReviewResultCode.NotFound);

        if (!RequestStatusTransitions.CanTransition(request.Status, dto.Status))
            return new ReviewResult(ReviewResultCode.InvalidTransition);

        var now = DateTime.UtcNow;
        var history = new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            AssistanceRequestId = id,
            FromStatus = request.Status,
            ToStatus = dto.Status,
            ChangedByUserId = userId,
            ChangedAt = now,
            Note = dto.Reason?.Trim()
        };
        await _repository.SaveStatusChangeAsync(history);
        await SendStatusNotificationSafelyAsync(
            id,
            history.Id,
            dto.Status,
            dto.Reason);

        return new ReviewResult(ReviewResultCode.Success, ToDto(request));
    }

    private async Task SendStatusNotificationSafelyAsync(
        Guid requestId,
        Guid eventId,
        RequestStatus status,
        string? note)
    {
        if (_notificationService is null)
        {
            _logger.LogWarning(
                "Assistance request {RequestId} reached status {Status}, but notification was skipped because the notification service is not configured.",
                requestId,
                status);
            return;
        }

        try
        {
            var recipient = await _repository.GetNotificationRecipientAsync(requestId);
            if (string.IsNullOrWhiteSpace(recipient))
            {
                _logger.LogWarning(
                    "Assistance request notification was skipped for {RequestId} at status {Status}: recipient lookup returned null or whitespace.",
                    requestId,
                    status);
                return;
            }

            var result = await _notificationService.SendAssistanceRequestStatusAsync(
                new AssistanceRequestStatusNotificationRequest
                {
                    EventId = eventId,
                    Recipient = recipient,
                    Channel = NotificationChannel.Email,
                    AssistanceRequestId = requestId,
                    Status = status,
                    Note = note
                },
                // Attempt delivery after the status save even if the HTTP request is cancelled.
                CancellationToken.None);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Assistance request notification completed for {RequestId} with {DeliveryStatus}; correlation {CorrelationId}.",
                    requestId,
                    result.Status,
                    result.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "Assistance request notification failed for {RequestId} with {DeliveryStatus}: {ErrorCode} ({ErrorMessage}), correlation {CorrelationId}. The status change remains saved.",
                    requestId,
                    result.Status,
                    result.ErrorCode,
                    result.ErrorMessage,
                    result.CorrelationId);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Assistance request notification dispatch failed for {RequestId} after its status change was saved.",
                requestId);
        }
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