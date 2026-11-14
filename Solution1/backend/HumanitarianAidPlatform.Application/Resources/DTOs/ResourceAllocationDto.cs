
using System;

namespace HumanitarianAidPlatform.Application.Resources.DTOs;

public class ResourceAllocationDto
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }

    public Guid AssistanceRequestId { get; set; }

    public int QuantityAllocated { get; set; }

    public DateTime AllocatedAt { get; set; }
}
