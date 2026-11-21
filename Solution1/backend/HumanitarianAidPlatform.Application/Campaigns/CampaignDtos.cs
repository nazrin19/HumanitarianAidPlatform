using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Campaigns;

public class CreateCampaignDto : IValidatableObject
{
    [Required, StringLength(150)]
    public string Name { get; set; } = default!;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, 1_000_000_000)]
    public decimal TargetAmount { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (EndDate.HasValue && EndDate.Value < StartDate)
            yield return new ValidationResult(
                "EndDate cannot be before StartDate.", new[] { nameof(EndDate) });
    }
}

public class UpdateCampaignDto : CreateCampaignDto
{
    public bool? IsActive { get; set; } 
}

public record CampaignResponseDto(
    Guid Id,
    string Name,
    string? Description,
    decimal TargetAmount,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive);