using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Donors;

public class UpdateDonorDto : CreateDonorDto
{
    
    public bool? IsActive { get; set; }
}