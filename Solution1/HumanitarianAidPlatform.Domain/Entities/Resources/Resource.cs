using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Resources;

public class Resource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public string? Description { get; set; }
}