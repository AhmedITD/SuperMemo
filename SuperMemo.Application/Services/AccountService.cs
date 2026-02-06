using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.responses.Accounts;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Accounts;

namespace SuperMemo.Application.Services;

public class AccountService(ISuperMemoDbContext db) : IAccountService
{
    public async Task<ApiResponse<AccountResponse>> GetMyAccountAsync(int userId, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
        if (account == null)
            return ApiResponse<AccountResponse>.ErrorResponse("Account not found. Your account may not be approved yet.", code: ErrorCodes.ResourceNotFound);

        return ApiResponse<AccountResponse>.SuccessResponse(Map(account));
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
            CreatedAt = a.CreatedAt
        };
    }
}
