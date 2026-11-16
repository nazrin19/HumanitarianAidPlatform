using System.ComponentModel.DataAnnotations;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public class CreateRequestDto
{
    [Required]
    public RequestCategory? Category { get; set; } // nullable so a missing value gives a 400, not a silent "Food"

    [Required, StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = default!;

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }
}