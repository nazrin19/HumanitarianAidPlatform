namespace HumanitarianAidPlatform.Application.Admin;

/// <summary>Safe account details returned to an administrator.</summary>
public sealed record AdminAccountResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string AccountStatus,
    DateTimeOffset CreatedAt);
