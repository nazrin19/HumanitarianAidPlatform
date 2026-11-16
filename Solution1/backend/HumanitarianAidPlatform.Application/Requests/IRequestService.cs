using HumanitarianAidPlatform.Application.Common;

namespace HumanitarianAidPlatform.Application.Requests;

public interface IRequestService
{
    Task<RequestResponseDto> SubmitAsync(Guid citizenId, CreateRequestDto dto);
    Task<RequestResponseDto?> GetByIdAsync(Guid id, Guid requesterId, string requesterRole);
    Task<IReadOnlyList<RequestStatusHistoryDto>?> GetHistoryAsync(Guid id, Guid requesterId, string requesterRole);
    Task<RecommendationDto?> GetRecommendationAsync(Guid id, string requesterRole);
    Task<PagedResult<RequestListItemDto>> GetMineAsync(
        Guid citizenId,
        RequestListQuery query);
    Task<PagedResult<RequestListItemDto>> GetForStaffAsync(
        RequestListQuery query);
}