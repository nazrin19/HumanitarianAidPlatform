using System.Security.Claims;

namespace HumanitarianAidPlatform.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static string? GetRole(this ClaimsPrincipal user) => user.FindFirst("role")?.Value;
}
