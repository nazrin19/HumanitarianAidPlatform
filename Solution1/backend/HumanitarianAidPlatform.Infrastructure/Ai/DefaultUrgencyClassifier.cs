using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Application.Ai;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Infrastructure.Ai;

// Cautious fallback: makes no judgement and asks a human to look at every request.
public class DefaultUrgencyClassifier : IUrgencyClassifier
{
    public string Name => "Default-v1";

    public Task<ClassificationResult> ClassifyAsync(AssistanceRequest request, CancellationToken cancellationToken = default)
    {
        var result = new ClassificationResult(
            request.Category,
            50,
            UrgencyLevel.Medium,
            0.30,
            "Fallback classifier used: no analysis was performed. Manual review required.");

        return Task.FromResult(result);
    }
}
