namespace HumanitarianAidPlatform.Infrastructure.External;

public static class RecipientMasking
{
    public static string Mask(string? recipient)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return "[blank]";
        }

        var trimmed = recipient.Trim();
        var atIndex = trimmed.IndexOf('@');
        if (atIndex > 0)
        {
            var visible = Math.Min(2, atIndex);
            var domainIndex = trimmed.IndexOf('@', atIndex);
            var domain = trimmed[(domainIndex)..];
            return $"{trimmed[..visible]}***{domain}";
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length >= 4)
        {
            return $"***{digits[^4..]}";
        }

        return "***";
    }
}
