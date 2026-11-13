using System;
using System.Collections.Generic;
using System.Text;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Ai;

// Strategy pattern: any classifier (rules, a real AI service, a fallback) can be plugged in.
public interface IUrgencyClassifier
{
    string Name { get; }
    Task<ClassificationResult> ClassifyAsync(AssistanceRequest request, CancellationToken cancellationToken = default);
}
