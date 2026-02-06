namespace SuperMemo.Domain.Exceptions;

/// <summary>
/// Thrown when a transaction has high fraud risk and requires admin review.
/// </summary>
public class HighRiskTransactionException : DomainException
{
    public HighRiskTransactionException(string message) : base(message)
    {
    }
}
