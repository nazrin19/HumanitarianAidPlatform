using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Application.Common;

namespace HumanitarianAidPlatform.Application.Requests;

public interface IRequestRepository
{
    Task AddAsync(AssistanceRequest request);
    Task<AssistanceRequest?> GetByIdAsync(Guid id);
    Task<AssistanceRequest?> GetTrackedByIdAsync(Guid id);
    Task<string?> GetNotificationRecipientAsync(Guid requestId);
    Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(Guid requestId);
    Task AttachRecommendationAsync(Guid requestId, AiRecommendation recommendation, RequestStatusHistory history);
    Task SaveReviewAsync(
        RequestStatusHistory history,
        AuditLog auditLog);
    Task SaveStatusChangeAsync(RequestStatusHistory history);
    Task<AiRecommendation?> GetRecommendationAsync(Guid requestId);
    Task<PagedResult<RequestListItemDto>> GetMineAsync(
        Guid citizenId,
        RequestListQuery query);
    Task<PagedResult<RequestListItemDto>> GetForStaffAsync(
        RequestListQuery query);
}