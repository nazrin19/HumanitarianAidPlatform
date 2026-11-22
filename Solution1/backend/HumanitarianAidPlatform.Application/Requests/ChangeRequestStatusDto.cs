using System.ComponentModel.DataAnnotations;
using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public sealed class ChangeRequestStatusDto
{
    [Required]
    public RequestStatus Status { get; init; }

    [StringLength(1000)]
    public string? Reason { get; init; }
}
