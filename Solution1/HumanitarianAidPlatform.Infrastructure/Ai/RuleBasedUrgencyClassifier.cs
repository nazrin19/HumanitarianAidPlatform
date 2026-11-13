using System;
using System.Collections.Generic;
using System.Text;

using System.Text.RegularExpressions;
using HumanitarianAidPlatform.Application.Ai;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Infrastructure.Ai;

public class RuleBasedUrgencyClassifier : IUrgencyClassifier
{
    public string Name => "RuleBased-v1";

    private static readonly Dictionary<RequestCategory, int> BaseScores = new()
    {
        [RequestCategory.Medical] = 60,
        [RequestCategory.Medicine] = 50,
        [RequestCategory.Shelter] = 45,
        [RequestCategory.Food] = 40,
        [RequestCategory.Transport] = 35,
        [RequestCategory.Other] = 25
    };

    private static readonly string[] CriticalWords =
    {
        "bleeding", "unconscious", "not breathing", "cannot breathe", "can't breathe",
        "heart attack", "stroke", "trapped", "drowning", "collapsed", "choking", "seizure"
    };

    private static readonly string[] HighWords =
    {
        "pregnant", "newborn", "baby", "infant", "elderly", "disabled", "wheelchair", "insulin",
        "diabetic", "oxygen", "dialysis", "injured", "fracture", "dehydrated", "children", "child",
        "no water", "flood", "landslide"
    };

    private static readonly Dictionary<RequestCategory, string[]> CategoryWords = new()
    {
        [RequestCategory.Food] = new[] { "food", "hungry", "hunger", "meal", "rice", "starving" },
        [RequestCategory.Medicine] = new[] { "medicine", "medication", "insulin", "tablets", "prescription", "pills" },
        [RequestCategory.Medical] = new[] { "injured", "bleeding", "doctor", "hospital", "ambulance", "fracture", "unconscious", "wound" },
        [RequestCategory.Shelter] = new[] { "shelter", "homeless", "roof", "evacuate", "no home" },
        [RequestCategory.Transport] = new[] { "transport", "vehicle", "stranded", "evacuation", "bus", "boat" }
    };

    private static readonly Dictionary<string, int> NumberWords = new()
    {
        ["three"] = 3,
        ["four"] = 4,
        ["five"] = 5,
        ["six"] = 6,
        ["seven"] = 7,
        ["eight"] = 8,
        ["nine"] = 9,
        ["ten"] = 10
    };

    public Task<ClassificationResult> ClassifyAsync(AssistanceRequest request, CancellationToken cancellationToken = default)
    {
        var text = request.Description.Replace('\u2019', '\'').ToLowerInvariant();
        var reasons = new List<string>();

        // 1. Base score from the category the citizen chose
        var score = BaseScores[request.Category];
        reasons.Add($"Base score {score} for category {request.Category}");

        // 2. Keyword signals (capped so one long description cannot dominate)
        var critical = CriticalWords.Where(w => ContainsWord(text, w)).ToList();
        var high = HighWords.Where(w => ContainsWord(text, w)).ToList();
        var keywordBoost = Math.Min(critical.Count * 30 + high.Count * 10, 45);
        if (critical.Count > 0) reasons.Add($"Critical terms found: {string.Join(", ", critical)}");
        if (high.Count > 0) reasons.Add($"Risk terms found: {string.Join(", ", high)}");
        if (keywordBoost > 0) reasons.Add($"Keyword boost +{keywordBoost}");
        score += keywordBoost;

        // 3. How long the problem has lasted
        if (Regex.IsMatch(text, @"\b(\d+|two|three|four|five|six|seven|several|many)\s+days?\b"))
        {
            score += 10;
            reasons.Add("Problem has lasted several days (+10)");
        }

        // 4. Household size
        var people = ExtractHouseholdSize(text);
        if (people >= 5)
        {
            score += 10;
            reasons.Add($"Large household of {people} (+10)");
        }

        score = Math.Clamp(score, 0, 100);

        // 5. Does the description suggest a different category?
        var hits = CategoryWords.ToDictionary(kv => kv.Key, kv => kv.Value.Count(w => ContainsWord(text, w)));
        var best = hits.OrderByDescending(kv => kv.Value).First();
        var suggested = best.Value > 0 && best.Value > hits.GetValueOrDefault(request.Category)
            ? best.Key
            : request.Category;
        if (suggested != request.Category)
            reasons.Add($"Description suggests category {suggested} rather than {request.Category}");

        // 6. Confidence: more matching signals and agreement raise it, vague text lowers it
        var signals = critical.Count + high.Count + hits.Values.Sum();
        var confidence = 0.5 + Math.Min(signals * 0.05, 0.25);
        confidence += suggested == request.Category ? 0.10 : -0.10;
        if (request.Description.Length < 30) confidence -= 0.10;
        confidence = Math.Round(Math.Clamp(confidence, 0.30, 0.95), 2);

        var level = score >= 80 ? UrgencyLevel.Critical
                  : score >= 60 ? UrgencyLevel.High
                  : score >= 40 ? UrgencyLevel.Medium
                  : UrgencyLevel.Low;

        var result = new ClassificationResult(suggested, score, level, confidence, string.Join("; ", reasons));
        return Task.FromResult(result);
    }

    private static bool ContainsWord(string text, string word) =>
        Regex.IsMatch(text, $@"\b{Regex.Escape(word)}(s|es|ed|ing)?\b", RegexOptions.IgnoreCase);

    private static int? ExtractHouseholdSize(string text)
    {
        var match = Regex.Match(text, @"\bfamily of (\d+|three|four|five|six|seven|eight|nine|ten)\b");
        if (!match.Success)
            match = Regex.Match(text, @"\b(\d+)\s+(people|persons|children|members)\b");
        if (!match.Success)
            return null;

        var value = match.Groups[1].Value;
        if (int.TryParse(value, out var n)) return n;
        return NumberWords.TryGetValue(value, out var word) ? word : null;
    }
}
