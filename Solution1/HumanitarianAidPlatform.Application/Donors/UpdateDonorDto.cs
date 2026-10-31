using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Donors;

public class UpdateDonorDto : CreateDonorDto
{
    [Required]
    public bool IsActive { get; set; }
}