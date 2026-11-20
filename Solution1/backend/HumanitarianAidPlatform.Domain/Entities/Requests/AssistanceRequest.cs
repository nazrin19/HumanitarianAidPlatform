using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Requests;

public class AssistanceRequest
{
    public Guid Id { get; set; }
    public Guid CitizenId { get; set; }
    public RequestCategory Category { get; set; }
    public string Description { get; set; } = default!;
    public RequestStatus Status { get; set; } = RequestStatus.Submitted;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RequestStatusHistory> StatusHistory { get; set; } = new List<RequestStatusHistory>();
}