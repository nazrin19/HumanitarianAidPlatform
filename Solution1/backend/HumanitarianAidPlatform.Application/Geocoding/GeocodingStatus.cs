namespace HumanitarianAidPlatform.Application.Geocoding;

public enum GeocodingStatus
{
    Found,
    NotFound,
    Ambiguous,
    ProviderUnavailable,
    InvalidInput,
    Cancelled
}
