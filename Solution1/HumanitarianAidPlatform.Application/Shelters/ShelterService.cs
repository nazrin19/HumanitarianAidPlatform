
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

        return shelters.Select(s => new ShelterDto
        {
            Id = s.Id,
            Name = s.Name,
            Capacity = s.Capacity,
            CurrentOccupancy = s.CurrentOccupancy,
            Latitude = s.Latitude,
            Longitude = s.Longitude
        }).ToList();
    }
}
