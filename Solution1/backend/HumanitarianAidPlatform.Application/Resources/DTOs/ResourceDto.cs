using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Resources.DTOs;

public class ResourceDto : IValidatableObject
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int QuantityAvailable { get; set; }

    public string? Description { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Latitude.HasValue != Longitude.HasValue)
        {
            yield return new ValidationResult(
                "Latitude and longitude must be provided together.",
                new[] { nameof(Latitude), nameof(Longitude) });
        }
    }
}