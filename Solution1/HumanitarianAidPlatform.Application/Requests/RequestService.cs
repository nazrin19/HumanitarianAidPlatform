using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Domain.Entities.Requests;

namespace HumanitarianAidPlatform.Application.Requests;

public class RequestService : IRequestService
{
    private readonly IRequestRepository _repository;

    public RequestService(IRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<RequestResponseDto> SubmitAsync(CreateRequestDto dto)
    {
        var request = new AssistanceRequest
        {
            Id = Guid.NewGuid(),
            CitizenId = dto.CitizenId,
            Category = dto.Category.Trim(),
            Description = dto.Description.Trim(),
            Status = "Submitted",
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(request);
        return ToDto(request);
    }

    public async Task<RequestResponseDto?> GetByIdAsync(Guid id)
    {
        var request = await _repository.GetByIdAsync(id);
        return request is null ? null : ToDto(request);
    }

    private static RequestResponseDto ToDto(AssistanceRequest r) =>
        new(r.Id, r.Category, r.Description, r.Status, r.Latitude, r.Longitude, r.Address, r.CreatedAt);
}
