namespace HumanitarianAidPlatform.Application.Auth;

public sealed record RegistrationResult(bool Succeeded, Guid? UserId, string? Error)
{
    public static RegistrationResult Ok(Guid userId) => new(true, userId, null);
    public static RegistrationResult Fail(string error) => new(false, null, error);
}
