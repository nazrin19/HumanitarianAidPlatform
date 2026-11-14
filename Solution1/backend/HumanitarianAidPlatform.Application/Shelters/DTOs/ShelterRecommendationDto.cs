namespace HumanitarianAidPlatform.Application.Shelters.DTOs;

public class ShelterRecommendationDto
{
    public Guid ShelterId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int AvailableCapacity { get; set; }

    public double? DistanceKm { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}