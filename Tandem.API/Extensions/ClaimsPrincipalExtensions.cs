using System.Security.Claims;

namespace Tandem.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    // The id of the logged-in user, read from the token (the NameIdentifier claim written by JwtTokenGenerator).
    // Only called in [Authorize] actions, where a valid token always contains it
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("The token has no user id claim."));
}
