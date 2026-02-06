using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.responses.Analytics;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Analytics;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class AnalyticsService(ISuperMemoDbContext db) : IAnalyticsService
{
    public async Task<ApiResponse<AnalyticsOverviewResponse>> GetOverviewAsync(int userId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account == null)
            return ApiResponse<AnalyticsOverviewResponse>.ErrorResponse("Account not found.", code: ErrorCodes.ResourceNotFound);

        var totalBalance = account.Balance;

        // Calculate monthly growth rate
        var currentMonth = DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day);
        var previousMonth = currentMonth.AddMonths(-1);

        var currentMonthBalance = await GetBalanceAtDateAsync(account.Id, currentMonth, cancellationToken);
        var previousMonthBalance = await GetBalanceAtDateAsync(account.Id, previousMonth, cancellationToken);

        decimal? monthlyGrowthRate = null;
        if (previousMonthBalance > 0)
        {
            monthlyGrowthRate = ((currentMonthBalance - previousMonthBalance) / previousMonthBalance) * 100;
        }

        var response = new AnalyticsOverviewResponse
        {
            TotalBalance = totalBalance,
            MonthlyGrowthRate = monthlyGrowthRate
        };

        return ApiResponse<AnalyticsOverviewResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<AnalyticsTransactionsResponse>> GetTransactionsAsync(
        int userId, DateTime? startDate, DateTime? endDate, string? direction, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account == null)
            return ApiResponse<AnalyticsTransactionsResponse>.ErrorResponse("Account not found.", code: ErrorCodes.ResourceNotFound);

        var query = db.Transactions.AsQueryable();

        // Filter by account
        query = query.Where(t => t.FromAccountId == account.Id || t.ToAccountNumber == account.AccountNumber);

        // Filter by date range
        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        // Filter by direction
        if (!string.IsNullOrEmpty(direction))
        {
            var transactionType = direction.ToLower() == "credit" ? TransactionType.Credit : TransactionType.Debit;
            query = query.Where(t => t.TransactionType == transactionType);
        }

        var totalCredit = await query
            .Where(t => t.TransactionType == TransactionType.Credit)
            .SumAsync(t => t.Amount, cancellationToken);

        var totalDebit = await query
            .Where(t => t.TransactionType == TransactionType.Debit)
            .SumAsync(t => t.Amount, cancellationToken);

        var response = new AnalyticsTransactionsResponse
        {
            TotalCredit = totalCredit,
            TotalDebit = totalDebit,
            StartDate = startDate,
            EndDate = endDate
        };

        return ApiResponse<AnalyticsTransactionsResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<AnalyticsBalanceTrendResponse>> GetBalanceTrendAsync(int userId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account == null)
            return ApiResponse<AnalyticsBalanceTrendResponse>.ErrorResponse("Account not found.", code: ErrorCodes.ResourceNotFound);

        var monthlyBalances = new List<MonthlyBalanceDto>();
        var now = DateTime.UtcNow;

        for (int i = 5; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var balance = await GetBalanceAtDateAsync(account.Id, monthEnd, cancellationToken);
            monthlyBalances.Add(new MonthlyBalanceDto
            {
                Month = monthStart.ToString("yyyy-MM"),
                Balance = balance
            });
        }

        var response = new AnalyticsBalanceTrendResponse
        {
            MonthlyBalances = monthlyBalances
        };

        return ApiResponse<AnalyticsBalanceTrendResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<PaginatedListResponse<TransactionListItemResponse>>> GetTransactionsListAsync(
        int userId, int page, int pageSize, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account == null)
            return ApiResponse<PaginatedListResponse<TransactionListItemResponse>>.ErrorResponse("Account not found.", code: ErrorCodes.ResourceNotFound);

        var query = db.Transactions
            .Where(t => t.FromAccountId == account.Id || t.ToAccountNumber == account.AccountNumber);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionListItemResponse
            {
                Id = t.Id,
                Amount = t.Amount,
                TransactionType = t.TransactionType,
                Status = t.Status,
                Purpose = t.Purpose,
                ToAccountNumber = t.ToAccountNumber,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var response = new PaginatedListResponse<TransactionListItemResponse>(transactions, totalCount, page, pageSize);

        return ApiResponse<PaginatedListResponse<TransactionListItemResponse>>.SuccessResponse(response);
    }

    private async Task<decimal> GetBalanceAtDateAsync(int accountId, DateTime date, CancellationToken cancellationToken)
    {
        // Get initial balance (account creation balance, typically 0)
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account == null) return 0;

        var initialBalance = 0m; // Assuming accounts start at 0

        // Calculate net change from transactions up to the date
        var creditSum = await db.Transactions
            .Where(t => t.ToAccountNumber == account.AccountNumber 
                && t.TransactionType == TransactionType.Credit
                && t.CreatedAt <= date
                && t.Status == TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0;

        var debitSum = await db.Transactions
            .Where(t => t.FromAccountId == accountId 
                && t.TransactionType == TransactionType.Debit
                && t.CreatedAt <= date
                && t.Status == TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0;

        return initialBalance + creditSum - debitSum;
    }
}
