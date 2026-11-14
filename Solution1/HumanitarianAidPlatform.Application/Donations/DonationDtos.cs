using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Donations;

public class CreateDonationDto
{
    [Required]
    public Guid DonorId { get; set; }

    public Guid? CampaignId { get; set; }

    [Required, RegularExpression("^(Monetary|InKind)$")]
    public string Type { get; set; } = "Monetary";

    [Range(0.01, 1_000_000_000)]
    public decimal Amount { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "LKR";

    [StringLength(500)]
    public string? Description { get; set; }
}

public record DonationResponseDto(
    Guid Id,
    Guid DonorId,
    string DonorName,
    Guid? CampaignId,
    string? CampaignName,
    string Type,
    decimal Amount,
    string Currency,
    string? Description,
    DateTime DonatedAt);