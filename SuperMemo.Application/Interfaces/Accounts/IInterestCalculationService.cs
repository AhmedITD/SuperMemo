namespace SuperMemo.Application.Interfaces.Accounts;

public interface IInterestCalculationService
{
    Task<decimal> CalculateInterestAsync(int accountId, decimal balance, CancellationToken cancellationToken = default);
    Task<bool> ApplyInterestAsync(int accountId, CancellationToken cancellationToken = default);
    Task<bool> ShouldCalculateInterestAsync(int accountId, CancellationToken cancellationToken = default);
    Task<int> ProcessAllSavingsAccountsAsync(CancellationToken cancellationToken = default);
}
