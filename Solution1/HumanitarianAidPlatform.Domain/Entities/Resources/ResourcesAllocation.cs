using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Resources;

public class ResourceAllocation
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public Guid AssistanceRequestId { get; set; }
    public int QuantityAllocated { get; set; }
    public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;
}
