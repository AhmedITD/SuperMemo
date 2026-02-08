using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Accounts;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class BalanceBonusService(
    ISuperMemoDbContext db,
    IAuditEventLogger auditLogger) : IBalanceBonusService
{
    private const decimal BonusRate = 0.0001m; // 0.01%

    public async Task<int> ProcessAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var accounts = await db.Accounts
            .Where(a => a.Status == AccountStatus.Active && a.Balance > 0)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var account in accounts)
        {
            var alreadyDoneToday = await db.Transactions
                .AnyAsync(t => t.FromAccountId == account.Id
                    && t.Category == TransactionCategory.BalanceBonus
                    && t.CreatedAt >= today,
                    cancellationToken);
            if (alreadyDoneToday)
                continue;

            var bonusAmount = Math.Round(account.Balance * BonusRate, 4);
            if (bonusAmount <= 0)
                continue;

            var transaction = new Transaction
            {
                FromAccountId = account.Id,
                ToAccountNumber = account.AccountNumber,
                Amount = bonusAmount,
                TransactionType = TransactionType.Credit,
                Status = TransactionStatus.Completed,
                Category = TransactionCategory.BalanceBonus,
                Purpose = "Balance bonus 0.01%",
                StatusChangedAt = DateTime.UtcNow
            };
            db.Transactions.Add(transaction);
            account.Balance += bonusAmount;
            account.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            await auditLogger.LogAsync("Account", account.Id.ToString(), "BalanceBonusApplied",
                new { AccountId = account.Id, BonusAmount = bonusAmount, NewBalance = account.Balance }, cancellationToken);
            processed++;
        }

        return processed;
    }
}
