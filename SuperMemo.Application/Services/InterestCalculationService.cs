using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Accounts;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class InterestCalculationService(
    ISuperMemoDbContext db,
    IAuditEventLogger auditLogger) : IInterestCalculationService
{
    private const decimal InterestRate = 0.0001m; // 0.01% = 0.0001

    public Task<decimal> CalculateInterestAsync(int accountId, decimal balance, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(balance * InterestRate);
    }

    public async Task<bool> ApplyInterestAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.AccountType == AccountType.Savings, cancellationToken);

        if (account == null || account.Status != AccountStatus.Active)
            return false;

        // Check if interest was already calculated today
        var today = DateTime.UtcNow.Date;
        if (account.LastInterestCalculationDate?.Date == today)
            return false; // Already calculated today

        var interestAmount = await CalculateInterestAsync(accountId, account.Balance, cancellationToken);
        if (interestAmount <= 0)
            return false;

        // Create interest transaction
        var transaction = new Transaction
        {
            FromAccountId = 0, // System-generated (no from account)
            ToAccountNumber = account.AccountNumber,
            Amount = interestAmount,
            TransactionType = TransactionType.Credit,
            Status = TransactionStatus.Completed,
            Purpose = "Interest payment",
            StatusChangedAt = DateTime.UtcNow
        };
        db.Transactions.Add(transaction);

        // Update account balance
        account.Balance += interestAmount;
        account.LastInterestCalculationDate = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync("Account", account.Id.ToString(), "InterestApplied",
            new { AccountId = accountId, InterestAmount = interestAmount, NewBalance = account.Balance }, cancellationToken);

        return true;
    }

    public async Task<bool> ShouldCalculateInterestAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account == null || account.AccountType != AccountType.Savings || account.Status != AccountStatus.Active)
            return false;

        var today = DateTime.UtcNow.Date;
        return account.LastInterestCalculationDate?.Date != today;
    }

    public async Task<int> ProcessAllSavingsAccountsAsync(CancellationToken cancellationToken = default)
    {
        var savingsAccounts = await db.Accounts
            .Where(a => a.AccountType == AccountType.Savings && a.Status == AccountStatus.Active)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var account in savingsAccounts)
        {
            if (await ShouldCalculateInterestAsync(account.Id, cancellationToken))
            {
                if (await ApplyInterestAsync(account.Id, cancellationToken))
                    processed++;
            }
        }

        return processed;
    }
}
