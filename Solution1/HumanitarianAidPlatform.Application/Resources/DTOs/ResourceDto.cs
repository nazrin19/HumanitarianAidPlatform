using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Resources.DTOs;

public class ResourceDto
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int QuantityAvailable { get; set; }

    public string? Description { get; set; }
}