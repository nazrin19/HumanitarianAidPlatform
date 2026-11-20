using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public sealed record RequestListItemDto(
    Guid Id,
    Guid CitizenId,
    RequestCategory Category,
    string Description,
    RequestStatus Status,
    double? Latitude,
    double? Longitude,
    string? Address,
    DateTime CreatedAt,
    UrgencyLevel? UrgencyLevel,
    int? UrgencyScore,
    double? Confidence);
