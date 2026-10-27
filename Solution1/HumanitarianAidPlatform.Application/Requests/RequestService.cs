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
        var now = DateTime.UtcNow;

        var request = new AssistanceRequest
        {
            Id = Guid.NewGuid(),
            CitizenId = citizenId,
            Category = dto.Category!.Value,
            Description = dto.Description.Trim(),
            Status = RequestStatus.Submitted,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address?.Trim(),
            CreatedAt = now
        };

        // First timeline entry. Saved in the same database transaction as the request itself.
        request.StatusHistory.Add(new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            AssistanceRequestId = request.Id,
            FromStatus = null,
            ToStatus = RequestStatus.Submitted,
            ChangedByUserId = citizenId,
            ChangedAt = now,
            Note = "Request submitted"
        });

        await _repository.AddAsync(request);
        return ToDto(request);
    }

    public async Task<RequestResponseDto?> GetByIdAsync(Guid id, Guid requesterId, string requesterRole)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request is null || !CanView(request, requesterId, requesterRole))
            return null;

        return ToDto(request);
    }

    public async Task<IReadOnlyList<RequestStatusHistoryDto>?> GetHistoryAsync(Guid id, Guid requesterId, string requesterRole)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request is null || !CanView(request, requesterId, requesterRole))
            return null;

        var history = await _repository.GetHistoryAsync(id);
        return history
            .Select(h => new RequestStatusHistoryDto(h.FromStatus, h.ToStatus, h.ChangedByUserId, h.ChangedAt, h.Note))
            .ToList();
    }

    // Owner or staff only. Everyone else gets "not found" so they can't tell the request exists.
    private static bool CanView(AssistanceRequest request, Guid requesterId, string requesterRole) =>
        request.CitizenId == requesterId || StaffRoles.Contains(requesterRole);

    private static RequestResponseDto ToDto(AssistanceRequest r) =>
        new(r.Id, r.CitizenId, r.Category, r.Description, r.Status, r.Latitude, r.Longitude, r.Address, r.CreatedAt);
}