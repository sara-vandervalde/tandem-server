namespace Tandem.Core.Common;

// Each value is translated to one HTTP status code in the API layer
public enum ResultStatus
{
    Success,
    Unauthorized,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError
}
