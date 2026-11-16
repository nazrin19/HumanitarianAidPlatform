using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public record RequestResponseDto(
    Guid Id,
    Guid CitizenId,
    RequestCategory Category,
    string Description,
    RequestStatus Status,
    double? Latitude,
    double? Longitude,
    string? Address,
    DateTime CreatedAt);