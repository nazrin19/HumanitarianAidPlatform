namespace HumanitarianAidPlatform.Infrastructure.External;

public sealed class NominatimOptions
{
    public const string SectionName = "Geocoding:Nominatim";

    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org/";
    public string UserAgent { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;
    public int MaximumCandidates { get; set; } = 5;
    public int MaximumRetries { get; set; } = 2;
}
