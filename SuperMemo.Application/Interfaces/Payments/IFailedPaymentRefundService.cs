namespace SuperMemo.Application.Interfaces.Payments;

/// <summary>
/// Processes failed payments by refunding the amount to the payment's from account.
/// </summary>
public interface IFailedPaymentRefundService
{
    /// <summary>Finds payments with Status == Failed, credits the account, and marks them Refunded. Returns count processed.</summary>
    Task<int> ProcessFailedPaymentsAsync(CancellationToken cancellationToken = default);
}
