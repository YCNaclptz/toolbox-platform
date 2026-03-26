using System.Security.Claims;

namespace Platform.Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst("userId")?.Value
            ?? throw new InvalidOperationException("userId claim not found");
        return int.Parse(userIdClaim);
    }

    public static string GetUsername(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("username")?.Value
            ?? throw new InvalidOperationException("username claim not found");
    }

    public static string GetDisplayName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("displayName")?.Value
            ?? throw new InvalidOperationException("displayName claim not found");
    }

    public static string GetRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("role")?.Value
            ?? throw new InvalidOperationException("role claim not found");
    }

    public static List<string> GetAccessibleApps(this ClaimsPrincipal principal)
    {
        var appsClaim = principal.FindFirst("apps")?.Value;
        if (string.IsNullOrWhiteSpace(appsClaim))
            return new List<string>();
        
        return appsClaim.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
