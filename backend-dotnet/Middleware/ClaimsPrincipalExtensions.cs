using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace MyKopdes.Middleware;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirst("sub")?.Value
                   ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return int.TryParse(id, out var result) ? result : null;
    }

    public static int GetUserIdOrThrow(this ClaimsPrincipal user)
    {
        var id = user.FindFirst("sub")?.Value
                   ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (id != null && int.TryParse(id, out var result)) return result;
        throw new InvalidOperationException("User ID not found in token");
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value ?? "member";
    }

    public static string GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "";
    }

    public static string GetUserType(this ClaimsPrincipal user)
    {
        return user.FindFirst("type")?.Value ?? "user";
    }

    public static bool IsMember(this ClaimsPrincipal user)
    {
        return user.GetUserType() == "member";
    }

    public static bool IsStaff(this ClaimsPrincipal user)
    {
        return user.GetUserType() == "user";
    }
}
