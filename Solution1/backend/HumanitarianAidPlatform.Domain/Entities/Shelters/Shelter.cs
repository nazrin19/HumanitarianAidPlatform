using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Shelters;

public class Shelter
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}