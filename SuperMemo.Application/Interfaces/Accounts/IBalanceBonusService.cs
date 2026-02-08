namespace SuperMemo.Application.Interfaces.Accounts;

/// <summary>
/// Adds 0.01% of current balance to each active account (once per day).
/// </summary>
public interface IBalanceBonusService
{
    /// <summary>Credits 0.01% of balance to each active account that has not received today's bonus. Returns count processed.</summary>
    Task<int> ProcessAllAccountsAsync(CancellationToken cancellationToken = default);
}
