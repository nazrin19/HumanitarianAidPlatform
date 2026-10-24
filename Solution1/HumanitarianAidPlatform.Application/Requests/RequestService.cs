using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Domain.Entities.Users;

namespace HumanitarianAidPlatform.Application.Requests;

public class RequestService : IRequestService
{
    // Roles that may view any request. A citizen may only view their own.
    private static readonly HashSet<string> StaffRoles = new()
    {
        RoleNames.Coordinator,
        RoleNames.CaseManager,
        RoleNames.ResourceManager,
        RoleNames.ShelterAdmin,
        RoleNames.Admin
    };

    private readonly IRequestRepository _repository;

    public RequestService(IRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<RequestResponseDto> SubmitAsync(Guid citizenId, CreateRequestDto dto)
    {
        var request = new AssistanceRequest
        {
            Id = Guid.NewGuid(),
            CitizenId = citizenId,
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

    public async Task<RequestResponseDto?> GetByIdAsync(Guid id, Guid requesterId, string requesterRole)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request is null)
            return null;

        // Business-rule check: owner or staff only. Others get "not found" so they can't tell it exists.
        var isOwner = request.CitizenId == requesterId;
        if (!isOwner && !StaffRoles.Contains(requesterRole))
            return null;

        return ToDto(request);
    }

    private static RequestResponseDto ToDto(AssistanceRequest r) =>
        new(r.Id, r.CitizenId, r.Category, r.Description, r.Status, r.Latitude, r.Longitude, r.Address, r.CreatedAt);
}