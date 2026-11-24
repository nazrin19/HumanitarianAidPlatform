namespace HumanitarianAidPlatform.Application.Auth;

/// <summary>Confirms submission of an application that requires administrator review.</summary>
public sealed record RegistrationPendingResponse(Guid ApplicationId, string Status, string Message);
