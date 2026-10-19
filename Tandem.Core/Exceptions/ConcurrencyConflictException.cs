namespace Tandem.Core.Exceptions;

// Thrown by a repository when a save fails because someone else changed the same row first
// (the concurrency token no longer matched). It is defined in Core so services can catch it
// without referencing EF Core, whose DbUpdateConcurrencyException it replaces
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Exception innerException)
        : base("The row was changed by another user since it was loaded.", innerException)
    {
    }
}
