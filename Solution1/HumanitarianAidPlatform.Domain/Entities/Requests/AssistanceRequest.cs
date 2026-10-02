using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Requests;

public class AssistanceRequest
{
    public Guid Id { get; set; }
    public Guid CitizenId { get; set; }
    public string Category { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Status { get; set; } = "Submitted";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}