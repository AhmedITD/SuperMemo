namespace SuperMemo.Application.Interfaces.Accounts;

public interface IDailyLimitService
{
    Task<decimal> CalculateDailyLimitAsync(int accountId, CancellationToken cancellationToken = default);
    Task<decimal> GetDailySpentAsync(int accountId, CancellationToken cancellationToken = default);
    Task<decimal> GetRemainingLimitAsync(int accountId, CancellationToken cancellationToken = default);
    Task ResetDailyLimitAsync(int accountId, CancellationToken cancellationToken = default);
    Task<int> ResetAllDailyLimitsAsync(CancellationToken cancellationToken = default);
    Task<bool> CheckDailyLimitAsync(int accountId, decimal transactionAmount, CancellationToken cancellationToken = default);
    Task UpdateDailySpentAsync(int accountId, decimal amount, CancellationToken cancellationToken = default);
}
