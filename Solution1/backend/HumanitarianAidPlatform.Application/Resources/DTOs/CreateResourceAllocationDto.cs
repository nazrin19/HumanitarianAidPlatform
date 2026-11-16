
using System;
using System.ComponentModel.DataAnnotations;

namespace HumanitarianAidPlatform.Application.Resources.DTOs;

public class CreateResourceAllocationDto
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public Guid AssistanceRequestId { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Allocation quantity must be greater than zero.")]
    public int QuantityAllocated { get; set; }
}
