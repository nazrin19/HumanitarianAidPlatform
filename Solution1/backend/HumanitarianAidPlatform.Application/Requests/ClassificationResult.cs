using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Ai;

public record ClassificationResult(
    RequestCategory SuggestedCategory,
    int UrgencyScore,
    UrgencyLevel UrgencyLevel,
    double Confidence,
    string Rationale);
