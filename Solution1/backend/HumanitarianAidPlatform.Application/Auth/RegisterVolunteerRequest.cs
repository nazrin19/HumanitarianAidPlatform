using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Auth;

/// <summary>Information needed to apply for a volunteer account.</summary>
public sealed record RegisterVolunteerRequest
{
    [Required, StringLength(100)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }

    [Required, StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }

    [Phone, StringLength(30)]
    public string? Phone { get; init; }

    [StringLength(500)]
    public string? Skills { get; init; }
}
