
namespace HumanitarianAidPlatform.Application.Resources.DTOs;

public class ResourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public string? Description { get; set; }
}
