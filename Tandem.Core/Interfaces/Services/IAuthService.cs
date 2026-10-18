using Tandem.Core.Common;
using Tandem.Core.DTOs.Auth;
using Tandem.Core.DTOs.Users;

namespace Tandem.Core.Interfaces.Services;

public interface IAuthService
{
    Task<Result<UserDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}
