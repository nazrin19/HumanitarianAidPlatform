using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Requests;

public class RequestStatusHistory
{
    public Guid Id { get; set; }
    public Guid AssistanceRequestId { get; set; }
    public RequestStatus? FromStatus { get; set; } // null for the very first entry
    public RequestStatus ToStatus { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
