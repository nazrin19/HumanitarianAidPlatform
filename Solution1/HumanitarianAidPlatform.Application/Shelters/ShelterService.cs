using HumanitarianAidPlatform.Application.Shelters.DTOs;
using HumanitarianAidPlatform.Domain.Entities.Shelters;

namespace HumanitarianAidPlatform.Application.Shelters;

public class ShelterService
{
    private readonly IShelterRepository _repository;

    public ShelterService(IShelterRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ShelterDto>> GetAllAsync()
    {
        var shelters = await _repository.GetAllAsync();
        return shelters.Select(MapToDto).ToList();
    }

    public async Task<ShelterDto?> GetByIdAsync(Guid id)
    {
        var shelter = await _repository.GetByIdAsync(id);
        return shelter is null ? null : MapToDto(shelter);
    }

    public async Task<ShelterDto> CreateAsync(ShelterDto dto)
    {
        var shelter = new Shelter
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Capacity = dto.Capacity,
            CurrentOccupancy = dto.CurrentOccupancy,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

        await _repository.AddAsync(shelter);
        return MapToDto(shelter);
    }

    public async Task<ShelterDto?> UpdateAsync(Guid id, ShelterDto dto)
    {
        var shelter = await _repository.GetByIdAsync(id);

        if (shelter is null)
            return null;

        shelter.Name = dto.Name;
        shelter.Capacity = dto.Capacity;
        shelter.CurrentOccupancy = dto.CurrentOccupancy;
        shelter.Latitude = dto.Latitude;
        shelter.Longitude = dto.Longitude;

        await _repository.UpdateAsync(shelter);
        return MapToDto(shelter);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var shelter = await _repository.GetByIdAsync(id);

        if (shelter is null)
            return false;

        await _repository.DeleteAsync(shelter);
        return true;
    }

    public async Task<List<ShelterRecommendationDto>>
        GetRecommendationsAsync(
            double? latitude,
            double? longitude,
            int requiredCapacity = 1)
    {
        if (requiredCapacity < 1)
            throw new ArgumentOutOfRangeException(
                nameof(requiredCapacity),
                "Required capacity must be at least 1.");

        var shelters = await _repository.GetAllAsync();

        var recommendations = shelters
            .Select(s => new ShelterRecommendationDto
            {
                ShelterId = s.Id,
                Name = s.Name,
                AvailableCapacity = s.Capacity - s.CurrentOccupancy,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                DistanceKm =
                    latitude.HasValue &&
                    longitude.HasValue &&
                    s.Latitude.HasValue &&
                    s.Longitude.HasValue
                        ? CalculateDistanceKm(
                            latitude.Value,
                            longitude.Value,
                            s.Latitude.Value,
                            s.Longitude.Value)
                        : null
            })
            .Where(s =>
                s.AvailableCapacity >= requiredCapacity);

        if (latitude.HasValue && longitude.HasValue)
        {
            return recommendations
                .OrderBy(s => s.DistanceKm ?? double.MaxValue)
                .ThenByDescending(s => s.AvailableCapacity)
                .ToList();
        }

        return recommendations
            .OrderByDescending(s => s.AvailableCapacity)
            .ToList();
    }

    private static ShelterDto MapToDto(Shelter shelter)
    {
        return new ShelterDto
        {
            Id = shelter.Id,
            Name = shelter.Name,
            Capacity = shelter.Capacity,
            CurrentOccupancy = shelter.CurrentOccupancy,
            Latitude = shelter.Latitude,
            Longitude = shelter.Longitude
        };
    }

    private static double CalculateDistanceKm(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        const double EarthRadiusKm = 6371.0;

        var lat1 = latitude1 * Math.PI / 180.0;
        var lat2 = latitude2 * Math.PI / 180.0;
        var deltaLat = (latitude2 - latitude1) * Math.PI / 180.0;
        var deltaLon = (longitude2 - longitude1) * Math.PI / 180.0;

        var a =
            Math.Pow(Math.Sin(deltaLat / 2.0), 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Pow(Math.Sin(deltaLon / 2.0), 2);

        var centralAngle = 2.0 * Math.Asin(
            Math.Sqrt(Math.Clamp(a, 0.0, 1.0)));

        return EarthRadiusKm * centralAngle;
    }
}