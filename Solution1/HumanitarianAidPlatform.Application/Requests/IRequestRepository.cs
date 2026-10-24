using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public interface IRequestRepository
{
    Task AddAsync(AssistanceRequest request);
    Task<AssistanceRequest?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(Guid requestId);
}