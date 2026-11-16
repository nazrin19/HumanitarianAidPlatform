using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Requests;

public class AiRecommendation
{
    public Guid Id { get; set; }
    public Guid AssistanceRequestId { get; set; }
    public RequestCategory SuggestedCategory { get; set; }
    public int UrgencyScore { get; set; }          // 0 to 100
    public UrgencyLevel UrgencyLevel { get; set; }
    public double Confidence { get; set; }         // 0.0 to 1.0
    public string Rationale { get; set; } = default!;
    public string ModelName { get; set; } = default!; // which classifier produced this
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
