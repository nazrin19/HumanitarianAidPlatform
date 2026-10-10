
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Requests;

public class CreateRequestDto
{
    [Required]
    public Guid CitizenId { get; set; } // temporary: will come from the JWT once auth is added

    [Required, StringLength(50)]
    public string Category { get; set; } = default!;

    [Required, StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = default!;

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }
}


