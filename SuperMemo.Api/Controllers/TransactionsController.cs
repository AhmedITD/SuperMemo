using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Transactions;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Transactions;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Api.Controllers;

[Authorize]
[Route("api/transactions")]
public class TransactionsController(ITransactionService transactionService, Application.Interfaces.Auth.ICurrentUser currentUser) : BaseController
{
    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>>> CreateTransfer(
        [FromBody] CreateTransferRequest request, CancellationToken cancellationToken)
    {
        request.IdempotencyKey ??= Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();
        var result = await transactionService.CreateTransferAsync(request, currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("account/{accountId}")]
    public async Task<ActionResult<ApiResponse<PaginatedListResponse<Application.DTOs.responses.Transactions.TransactionResponse>>>> ListByAccount(
        int accountId, [FromQuery] TransactionStatus? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await transactionService.ListByAccountAsync(accountId, currentUser.Id, status, pageNumber, pageSize, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await transactionService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
