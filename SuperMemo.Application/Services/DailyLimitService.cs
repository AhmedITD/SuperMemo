using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Accounts;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class DailyLimitService(
    ISuperMemoDbContext db,
    IAuditEventLogger auditLogger) : IDailyLimitService
{
    private const decimal SavingsDailyLimitPercentage = 0.05m; // 5%

    public async Task<decimal> CalculateDailyLimitAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account == null || account.AccountType != AccountType.Savings)
            return 0;

        return account.Balance * SavingsDailyLimitPercentage;
    }

    public async Task<decimal> GetDailySpentAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account == null || account.AccountType != AccountType.Savings)
            return 0;

        // Check if we need to reset (new day)
        var today = DateTime.UtcNow.Date;
        if (account.LastDailyLimitResetDate?.Date != today)
        {
            await ResetDailyLimitAsync(accountId, cancellationToken);
            return 0;
        }

        return account.DailySpentAmount;
    }

    public async Task<decimal> GetRemainingLimitAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var limit = await CalculateDailyLimitAsync(accountId, cancellationToken);
        var spent = await GetDailySpentAsync(accountId, cancellationToken);
        return Math.Max(0, limit - spent);
    }

    public async Task ResetDailyLimitAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account == null || account.AccountType != AccountType.Savings)
            return;

        var newLimit = account.Balance * SavingsDailyLimitPercentage;
        account.DailySpendingLimit = newLimit;
        account.DailySpentAmount = 0;
        account.LastDailyLimitResetDate = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync("Account", account.Id.ToString(), "DailyLimitReset",
            new { AccountId = accountId, NewLimit = newLimit }, cancellationToken);
    }

    public async Task<int> ResetAllDailyLimitsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var savingsAccounts = await db.Accounts
            .Where(a => a.AccountType == AccountType.Savings 
                && a.Status == AccountStatus.Active
                && (a.LastDailyLimitResetDate == null || a.LastDailyLimitResetDate.Value.Date != today))
            .ToListAsync(cancellationToken);

        var reset = 0;
        foreach (var account in savingsAccounts)
        {
            await ResetDailyLimitAsync(account.Id, cancellationToken);
            reset++;
        }

        return reset;
    }

    public async Task<bool> CheckDailyLimitAsync(int accountId, decimal transactionAmount, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account == null || account.AccountType != AccountType.Regular)
            return true; // Regular accounts have no limit

        if (account.AccountType != AccountType.Savings)
            return true;

        var spent = await GetDailySpentAsync(accountId, cancellationToken);
        var limit = await CalculateDailyLimitAsync(accountId, cancellationToken);

        return (spent + transactionAmount) <= limit;
    }

    public async Task UpdateDailySpentAsync(int accountId, decimal amount, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account == null || account.AccountType != AccountType.Savings)
            return;

        // Ensure daily limit is reset if needed
        var today = DateTime.UtcNow.Date;
        if (account.LastDailyLimitResetDate?.Date != today)
        {
            await ResetDailyLimitAsync(accountId, cancellationToken);
            account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
            if (account == null) return;
        }

        account.DailySpentAmount += amount;
        await db.SaveChangesAsync(cancellationToken);
    }
}
