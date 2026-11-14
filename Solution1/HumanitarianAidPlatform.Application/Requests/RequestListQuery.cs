using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public sealed class RequestListQuery
{
    public RequestStatus? Status { get; init; }
    public UrgencyLevel? Urgency { get; init; }
    public RequestCategory? Category { get; init; }
    public bool? Overdue { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
