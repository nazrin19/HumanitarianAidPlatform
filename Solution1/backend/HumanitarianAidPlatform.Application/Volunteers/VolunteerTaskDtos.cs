using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Volunteers;

public class CreateVolunteerTaskDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = default!;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public DateTime? DueDate { get; set; }
}

public class AssignVolunteerTaskDto
{
    [Required]
    public Guid VolunteerId { get; set; }
}

public record VolunteerTaskResponseDto(
    Guid Id,
    Guid? VolunteerId,
    string? VolunteerName,
    string Title,
    string? Description,
    string? Location,
    string Status,
    DateTime? DueDate,
    DateTime CreatedAt);