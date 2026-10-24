using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Requests;

public enum RequestStatus
{
    Submitted,
    PendingReview,
    Approved,
    Assigned,
    Fulfilled,
    Rejected
}
