namespace HumanitarianAidPlatform.Application.Requests;

public enum ReviewResultCode
{
    Success,
    NotFound,
    InvalidTransition,
    ReasonRequired,
    CriticalCoordinatorRequired
}

public sealed record ReviewResult(
    ReviewResultCode Code,
    RequestResponseDto? Request = null);
