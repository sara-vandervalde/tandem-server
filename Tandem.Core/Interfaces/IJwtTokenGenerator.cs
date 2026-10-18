using Tandem.Core.Entities;

namespace Tandem.Core.Interfaces;

// Creates the signed token a user receives after logging in.
// An interface, so AuthService can be unit-tested without real keys or crypto
public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(User user);
}
