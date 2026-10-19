using System.Security.Claims;

namespace Tandem.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    // The id of the logged-in user, read from the token (the NameIdentifier claim written by JwtTokenGenerator).
    // Only called in [Authorize] actions, where a valid token always contains it
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("The token has no user id claim."));

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)
        ?? throw new InvalidOperationException("The token has no email claim.");
}
