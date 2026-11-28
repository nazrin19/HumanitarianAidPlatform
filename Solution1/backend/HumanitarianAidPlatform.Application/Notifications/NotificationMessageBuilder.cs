using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Notifications;

public sealed record NotificationContent(string Title, string Message);

public static class NotificationMessageBuilder
{
    public static bool IsSupportedLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ||
        language.Equals("en", StringComparison.OrdinalIgnoreCase) ||
        language.Equals("si", StringComparison.OrdinalIgnoreCase) ||
        language.Equals("ta", StringComparison.OrdinalIgnoreCase);

    public static string NormalizeLanguage(string? language) =>
        IsSupportedLanguage(language) &&
        !string.IsNullOrWhiteSpace(language)
            ? language.ToLowerInvariant()
            : "en";

    public static NotificationContent General(string? language) =>
        NormalizeLanguage(language) switch
        {
            "si" => new("මානුෂීය ආධාර යාවත්කාලීන කිරීම", "ඔබගේ ආධාර ඉල්ලීම සඳහා නව යාවත්කාලීනයක් ඇත."),
            "ta" => new("மனிதாபிமான உதவி புதுப்பிப்பு", "உங்கள் உதவி கோரிக்கைக்கு புதிய புதுப்பிப்பு உள்ளது."),
            _ => new("Humanitarian aid update", "There is a new update about your assistance request.")
        };

    public static NotificationContent ApplicationStatus(
        Guid requestId,
        RequestStatus status,
        string? note,
        string? language)
    {
        var shortId = requestId.ToString("N")[..8].ToUpperInvariant();
        var statusText = StatusLabel(status, NormalizeLanguage(language));
        var suffix = string.IsNullOrWhiteSpace(note) ? string.Empty : $" {note.Trim()}";
        return NormalizeLanguage(language) switch
        {
            "si" => new(
                "ආධාර ඉල්ලීමේ තත්ත්වය යාවත්කාලීනයි",
                $"ඔබගේ ඉල්ලීම {shortId} දැන් {statusText}.{suffix}"),
            "ta" => new(
                "உதவி கோரிக்கை நிலை புதுப்பிக்கப்பட்டது",
                $"உங்கள் கோரிக்கை {shortId} இப்போது {statusText}.{suffix}"),
            _ => new(
                "Assistance request status update",
                $"Your assistance request {shortId} is now {statusText}.{suffix}")
        };
    }

    public static NotificationContent Allocation(
        Guid requestId,
        int quantity,
        string? resourceName,
        string? assignmentName,
        string? language)
    {
        var languageCode = NormalizeLanguage(language);
        var resource = string.IsNullOrWhiteSpace(resourceName) ? "assistance" : resourceName.Trim();
        var assignment = string.IsNullOrWhiteSpace(assignmentName) ? string.Empty : $" ({assignmentName.Trim()})";
        var shortId = requestId.ToString("N")[..8].ToUpperInvariant();
        return languageCode switch
        {
            "si" => new(
                "ආධාර වෙන් කිරීම යාවත්කාලීනයි",
                $"ඉල්ලීම {shortId} සඳහා {quantity} {resource} වෙන් කර ඇත{assignment}."),
            "ta" => new(
                "உதவி ஒதுக்கீடு புதுப்பிக்கப்பட்டது",
                $"கோரிக்கை {shortId}க்கு {quantity} {resource} ஒதுக்கப்பட்டுள்ளது{assignment}."),
            _ => new(
                "Assistance allocation update",
                $"{quantity} {resource} has been allocated to request {shortId}{assignment}.")
        };
    }

    public static NotificationContent Emergency(string? message, string? language)
    {
        var template = NormalizeLanguage(language) switch
        {
            "si" => new NotificationContent("හදිසි නිවේදනය", "කරුණාකර මෙම වැදගත් යාවත්කාලීනය බලන්න."),
            "ta" => new NotificationContent("அவசர அறிவிப்பு", "இந்த முக்கியமான புதுப்பிப்பைப் பார்க்கவும்."),
            _ => new NotificationContent("Emergency announcement", "Please review this important update.")
        };
        return template with
        {
            Message = string.IsNullOrWhiteSpace(message) ? template.Message : message.Trim()
        };
    }

    private static string StatusLabel(RequestStatus status, string language) =>
        (status, language) switch
        {
            (RequestStatus.Submitted, "si") => "ඉදිරිපත් කර ඇත",
            (RequestStatus.PendingReview, "si") => "සමාලෝචනය සඳහා බලා සිටී",
            (RequestStatus.Approved, "si") => "අනුමත කර ඇත",
            (RequestStatus.Assigned, "si") => "කණ්ඩායමකට පවරා ඇත",
            (RequestStatus.Fulfilled, "si") => "සම්පූර්ණ කර ඇත",
            (RequestStatus.Rejected, "si") => "ප්‍රතික්ෂේප කර ඇත",
            (RequestStatus.Deferred, "si") => "කල් දමා ඇත",
            (RequestStatus.Submitted, "ta") => "சமர்ப்பிக்கப்பட்டது",
            (RequestStatus.PendingReview, "ta") => "மதிப்பாய்வுக்காக காத்திருக்கிறது",
            (RequestStatus.Approved, "ta") => "அங்கீகரிக்கப்பட்டது",
            (RequestStatus.Assigned, "ta") => "குழுவிற்கு ஒதுக்கப்பட்டது",
            (RequestStatus.Fulfilled, "ta") => "நிறைவு செய்யப்பட்டது",
            (RequestStatus.Rejected, "ta") => "நிராகரிக்கப்பட்டது",
            (RequestStatus.Deferred, "ta") => "ஒத்திவைக்கப்பட்டது",
            _ => status switch
            {
                RequestStatus.PendingReview => "awaiting review",
                RequestStatus.Assigned => "assigned to a team",
                _ => status.ToString().ToLowerInvariant()
            }
        };
}
