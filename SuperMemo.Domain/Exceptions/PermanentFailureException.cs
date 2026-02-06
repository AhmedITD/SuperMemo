namespace SuperMemo.Domain.Exceptions;

/// <summary>
/// Thrown when a transaction fails with a permanent error that should not be retried.
/// </summary>
public class PermanentFailureException : DomainException
{
    public PermanentFailureException(string message) : base(message)
    {
    }
}
