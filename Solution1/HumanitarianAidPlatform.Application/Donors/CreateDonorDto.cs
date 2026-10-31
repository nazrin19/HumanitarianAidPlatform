using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Donors;

public class CreateDonorDto
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = default!;

    [Phone, StringLength(30)]
    public string? Phone { get; set; }

    [Required, RegularExpression("^(Individual|Corporate)$")]
    public string Type { get; set; } = "Individual";
}