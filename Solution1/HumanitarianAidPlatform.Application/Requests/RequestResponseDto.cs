namespace HumanitarianAidPlatform.Application.Requests;

public record RequestResponseDto(
    Guid Id,
    Guid CitizenId,
    string Category,
    string Description,
    string Status,
    double? Latitude,
    double? Longitude,
    string? Address,
    DateTime CreatedAt);