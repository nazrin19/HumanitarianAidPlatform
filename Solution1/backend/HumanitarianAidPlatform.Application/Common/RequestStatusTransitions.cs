using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Common;

public static class RequestStatusTransitions
{
    private static readonly IReadOnlyDictionary<RequestStatus, IReadOnlySet<RequestStatus>> Allowed =
        new Dictionary<RequestStatus, IReadOnlySet<RequestStatus>>
        {
            [RequestStatus.Submitted] = new HashSet<RequestStatus>
            {
                RequestStatus.PendingReview
            },
            [RequestStatus.PendingReview] = new HashSet<RequestStatus>
            {
                RequestStatus.Approved,
                RequestStatus.Rejected,
                RequestStatus.Deferred
            },
            [RequestStatus.Approved] = new HashSet<RequestStatus>
            {
                RequestStatus.Assigned
            },
            [RequestStatus.Assigned] = new HashSet<RequestStatus>
            {
                RequestStatus.Fulfilled
            }
        };

    public static bool CanTransition(RequestStatus from, RequestStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}
