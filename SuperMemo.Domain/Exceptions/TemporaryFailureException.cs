namespace SuperMemo.Domain.Exceptions;

/// <summary>
/// Thrown when a transaction fails with a temporary error that can be retried.
/// </summary>
public class TemporaryFailureException : DomainException
{
    public int RetryAfterSeconds { get; }

    public TemporaryFailureException(string message, int retryAfterSeconds = 10) : base(message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
