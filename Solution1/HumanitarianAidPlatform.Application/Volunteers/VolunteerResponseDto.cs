namespace HumanitarianAidPlatform.Application.Volunteers;

public record VolunteerResponseDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? Skills,
    string Availability,
    DateTime CreatedAt);