namespace HumanitarianAidPlatform.Application.Admin;

/// <summary>Public account application awaiting an administrator decision.</summary>
public sealed record PendingAccountResponse(
    Guid Id,
    string FullName,
    string Email,
    string AccountType,
    string? Phone,
    string? Skills,
    string? DonorType,
    DateTimeOffset CreatedAt);
