using SuperMemo.Application.DTOs.responses.Accounts;
using SuperMemo.Application.DTOs.responses.Common;

namespace SuperMemo.Application.Interfaces.Accounts;

public interface IAccountService
{
    Task<ApiResponse<AccountResponse>> GetMyAccountAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<AccountResponse>> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
}
