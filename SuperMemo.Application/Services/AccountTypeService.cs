using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Accounts;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class AccountTypeService(ISuperMemoDbContext db) : IAccountTypeService
{
    public async Task<bool> IsSavingsAccountAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        return account?.AccountType == AccountType.Savings;
    }

    public async Task<bool> IsRegularAccountAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        return account?.AccountType == AccountType.Regular;
    }

    public async Task<AccountType> GetAccountTypeAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        return account?.AccountType ?? AccountType.Regular;
    }
}
