using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.responses.Dashboard;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Dashboard;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class DashboardService(ISuperMemoDbContext db) : IDashboardService
{
    public async Task<ApiResponse<DashboardResponse>> GetDashboardAsync(int userId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .Include(a => a.User)
            .Include(a => a.Cards)
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account == null)
            return ApiResponse<DashboardResponse>.ErrorResponse("Account not found.", code: ErrorCodes.ResourceNotFound);

        // Calculate totals
        var totalBalance = account.Balance;

        var totalDebit = await db.Transactions
            .Where(t => t.FromAccountId == account.Id && t.TransactionType == TransactionType.Debit)
            .SumAsync(t => t.Amount, cancellationToken);

        var totalCredit = await db.Transactions
            .Where(t => t.ToAccountNumber == account.AccountNumber && t.TransactionType == TransactionType.Credit)
            .SumAsync(t => t.Amount, cancellationToken);

        // Get recent transactions (last 10)
        var recentTransactions = await db.Transactions
            .Where(t => t.FromAccountId == account.Id || t.ToAccountNumber == account.AccountNumber)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new TransactionListItemDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Direction = t.TransactionType == TransactionType.Debit ? "debit" : "credit",
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Map cards
        var cards = account.Cards.Select(c => new Application.DTOs.responses.Cards.CardResponse
        {
            Id = c.Id,
            AccountId = c.AccountId,
            NumberMasked = "****" + c.Number[^4..],
            Type = c.Type,
            ExpiryDate = c.ExpiryDate,
            IsActive = c.IsActive,
            IsExpired = c.IsExpired,
            IsEmployeeCard = c.IsEmployeeCard,
            CreatedAt = c.CreatedAt
        }).ToList();

        var response = new DashboardResponse
        {
            TotalBalance = totalBalance,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            User = new UserInfoDto
            {
                Id = account.User.Id,
                Name = account.User.FullName,
                Phone = account.User.Phone,
                AccountCreatedAt = account.CreatedAt
            },
            Cards = cards,
            RecentTransactions = recentTransactions
        };

        return ApiResponse<DashboardResponse>.SuccessResponse(response);
    }
}
