using System;
using System.Collections.Generic;
using System.Text;


namespace HumanitarianAidPlatform.Application.Shelters.DTOs;

public class ShelterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

