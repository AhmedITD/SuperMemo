using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.responses.Accounts;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Accounts;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class AccountService(ISuperMemoDbContext db) : IAccountService
{
    public async Task<ApiResponse<AccountResponse>> GetMyAccountAsync(int userId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account != null)
            return ApiResponse<AccountResponse>.SuccessResponse(Map(account));

        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user == null)
            return ApiResponse<AccountResponse>.ErrorResponse("User not found.", code: ErrorCodes.ResourceNotFound);
        if (user.ApprovalStatus != ApprovalStatus.Approved)
            return ApiResponse<AccountResponse>.ErrorResponse("Account not found. Your account may not be approved yet.", code: ErrorCodes.ResourceNotFound);

        account = new Account
        {
            UserId = userId,
            Balance = 0,
            Currency = "IQD",
            Status = AccountStatus.Active,
            AccountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken),
            AccountType = AccountType.Regular
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);

        return ApiResponse<AccountResponse>.SuccessResponse(Map(account));
    }

    private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            var number = "SM" + Random.Shared.Next(100000000, 999999999).ToString();
            if (!await db.Accounts.AnyAsync(a => a.AccountNumber == number, cancellationToken))
                return number;
        }
        return "SM" + Guid.NewGuid().ToString("N")[..16];
    }

    public async Task<ApiResponse<AccountResponse>> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
        if (account == null)
            return ApiResponse<AccountResponse>.ErrorResponse("Account not found.", code: ErrorCodes.ResourceNotFound);

        return ApiResponse<AccountResponse>.SuccessResponse(Map(account));
    }

    private static AccountResponse Map(Domain.Entities.Account a)
    {
        return new AccountResponse
        {
            Id = a.Id,
            UserId = a.UserId,
            AccountNumber = a.AccountNumber,
            Balance = a.Balance,
            Currency = a.Currency,
            Status = a.Status,
            AccountType = a.AccountType,
            CreatedAt = a.CreatedAt
        };
    }
}
