using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ListamCompetitor.Api.Auth;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue("sub");

        if (raw == null || !Guid.TryParse(raw, out var id))
            throw new UnauthorizedAccessException("Invalid user id");

        return id;
    }
}