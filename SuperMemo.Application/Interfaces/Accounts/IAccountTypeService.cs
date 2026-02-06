using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Interfaces.Accounts;

public interface IAccountTypeService
{
    Task<bool> IsSavingsAccountAsync(int accountId, CancellationToken cancellationToken = default);
    Task<bool> IsRegularAccountAsync(int accountId, CancellationToken cancellationToken = default);
    Task<AccountType> GetAccountTypeAsync(int accountId, CancellationToken cancellationToken = default);
}
