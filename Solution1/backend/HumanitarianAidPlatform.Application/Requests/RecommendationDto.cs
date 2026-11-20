using System;
using System.Collections.Generic;
using System.Text;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public record RecommendationDto(
    RequestCategory SuggestedCategory,
    int UrgencyScore,
    UrgencyLevel UrgencyLevel,
    double Confidence,
    string Rationale,
    string ModelName,
    DateTime GeneratedAt);
