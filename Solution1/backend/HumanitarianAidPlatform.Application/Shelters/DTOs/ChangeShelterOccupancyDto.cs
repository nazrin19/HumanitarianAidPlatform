
using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Shelters.DTOs;

public class ChangeShelterOccupancyDto
{
    [Range(
        -int.MaxValue,
        int.MaxValue,
        ErrorMessage = "Occupancy change is outside the supported range.")]
    public int Change { get; set; }
}
