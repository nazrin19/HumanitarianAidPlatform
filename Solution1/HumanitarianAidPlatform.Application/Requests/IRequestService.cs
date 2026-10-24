namespace HumanitarianAidPlatform.Application.Requests;

public interface IRequestService
{
    Task<RequestResponseDto> SubmitAsync(Guid citizenId, CreateRequestDto dto);
    Task<RequestResponseDto?> GetByIdAsync(Guid id, Guid requesterId, string requesterRole);
}