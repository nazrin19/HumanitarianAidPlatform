using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Infrastructure.Ai;
using Xunit;

namespace HumanitarianAidPlatform.Tests;

public class RuleBasedUrgencyClassifierTests
{
    [Theory]
    [InlineData(RequestCategory.Food)]
    [InlineData(RequestCategory.Medicine)]
    [InlineData(RequestCategory.Shelter)]
    [InlineData(RequestCategory.Transport)]
    [InlineData(RequestCategory.Medical)]
    [InlineData(RequestCategory.Other)]
    public async Task CriticalIndicatorGetsCriticalReviewRegardlessOfSelectedCategory(
        RequestCategory category)
    {
        var classifier = new RuleBasedUrgencyClassifier();
        var request = new AssistanceRequest
        {
            Category = category,
            Description = "A person is drowning"
        };

        var result = await classifier.ClassifyAsync(request);

        Assert.Equal(UrgencyLevel.Critical, result.UrgencyLevel);
        Assert.True(result.UrgencyScore >= 80);
        Assert.Contains("critical review threshold", result.Rationale);
    }
}
