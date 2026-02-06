namespace SuperMemo.Domain.Exceptions;

/// <summary>
/// Thrown when attempting an invalid transaction status transition.
/// </summary>
public class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(string message) : base(message)
    {
    }
}
