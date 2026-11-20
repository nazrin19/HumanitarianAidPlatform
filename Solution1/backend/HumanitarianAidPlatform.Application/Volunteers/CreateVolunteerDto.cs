using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Volunteers;

public class CreateVolunteerDto
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = default!;

    [Phone, StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Skills { get; set; }

    [Required, RegularExpression("^(Available|Busy|Inactive)$")]
    public string Availability { get; set; } = "Available";
}