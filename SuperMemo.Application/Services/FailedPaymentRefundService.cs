using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Payments;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class FailedPaymentRefundService(
    ISuperMemoDbContext db,
    IAuditEventLogger auditLogger) : IFailedPaymentRefundService
{
    public async Task<int> ProcessFailedPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var failedPayments = await db.Payments
            .Include(p => p.Account)
            .Where(p => p.Status == PaymentStatus.Failed)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var payment in failedPayments)
        {
            if (payment.Account == null || payment.Amount <= 0)
                continue;
            if (payment.Account.Status != AccountStatus.Active)
                continue;

            var account = payment.Account;

            var transaction = new Transaction
            {
                FromAccountId = account.Id,
                ToAccountNumber = account.AccountNumber,
                Amount = payment.Amount,
                TransactionType = TransactionType.Credit,
                Status = TransactionStatus.Completed,
                Category = TransactionCategory.Refund,
                Purpose = $"Refund for failed payment (Payment #{payment.Id})",
                StatusChangedAt = DateTime.UtcNow
            };
            db.Transactions.Add(transaction);
            account.Balance += payment.Amount;
            account.UpdatedAt = DateTime.UtcNow;
            payment.Status = PaymentStatus.Refunded;
            payment.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            await auditLogger.LogAsync("Payment", payment.Id.ToString(), "RefundedToAccount",
                new { PaymentId = payment.Id, AccountId = account.Id, Amount = payment.Amount }, cancellationToken);
            processed++;
        }

        return processed;
    }
}
