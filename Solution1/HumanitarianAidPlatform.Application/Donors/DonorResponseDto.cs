namespace HumanitarianAidPlatform.Application.Donors;

public record DonorResponseDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string Type,
    bool IsActive,
    DateTime CreatedAt);