using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Shelters.DTOs;

public class ShelterDto : IValidatableObject
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    [Range(0, int.MaxValue)]
    public int CurrentOccupancy { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (CurrentOccupancy > Capacity)
        {
            yield return new ValidationResult(
                "Current occupancy cannot exceed total capacity.",
                new[] { nameof(CurrentOccupancy), nameof(Capacity) });
        }

        if (Latitude.HasValue != Longitude.HasValue)
        {
            yield return new ValidationResult(
                "Latitude and longitude must be provided together.",
                new[] { nameof(Latitude), nameof(Longitude) });
        }
    }
}