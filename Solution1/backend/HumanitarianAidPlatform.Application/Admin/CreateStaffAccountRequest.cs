using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Admin;

/// <summary>Request to create an active staff account.</summary>
public sealed record CreateStaffAccountRequest
{
    [Required, StringLength(100)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }

    [Required, StringLength(100, MinimumLength = 12)]
    public required string Password { get; init; }

    [Required]
    public required string Role { get; init; }
}
