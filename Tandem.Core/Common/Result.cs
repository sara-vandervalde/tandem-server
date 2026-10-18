namespace Tandem.Core.Common;

// The outcome of a service operation. Services return a Result instead of throwing for expected failures,
// and the controller translates the status to an HTTP response
public class Result
{
    protected Result(ResultStatus status, string? error)
    {
        Status = status;
        Error = error;
    }

    public ResultStatus Status { get; }

    // A user-facing message (in Hebrew) when the operation failed
    public string? Error { get; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result Success() => new(ResultStatus.Success, null);
    public static Result Unauthorized(string error) => new(ResultStatus.Unauthorized, error);
    public static Result NotFound(string error) => new(ResultStatus.NotFound, error);
    public static Result Forbidden(string error) => new(ResultStatus.Forbidden, error);
    public static Result Conflict(string error) => new(ResultStatus.Conflict, error);
    public static Result ValidationError(string error) => new(ResultStatus.ValidationError, error);
}

// A Result that also carries a value when the operation succeeded
public class Result<T> : Result
{
    private Result(T value) : base(ResultStatus.Success, null)
    {
        Value = value;
    }

    private Result(ResultStatus status, string error) : base(status, error)
    {
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Unauthorized(string error) => new(ResultStatus.Unauthorized, error);
    public static new Result<T> NotFound(string error) => new(ResultStatus.NotFound, error);
    public static new Result<T> Forbidden(string error) => new(ResultStatus.Forbidden, error);
    public static new Result<T> Conflict(string error) => new(ResultStatus.Conflict, error);
    public static new Result<T> ValidationError(string error) => new(ResultStatus.ValidationError, error);
}
