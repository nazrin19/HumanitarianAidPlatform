using System.ComponentModel.DataAnnotations;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public sealed class ReviewRequestDto
{
    [Required]
    public ReviewAction Action { get; init; }

    public RequestCategory? Category { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    [StringLength(2000)]
    public string? ChangedValues { get; init; }

    [StringLength(1000)]
    public string? Reason { get; init; }
}
