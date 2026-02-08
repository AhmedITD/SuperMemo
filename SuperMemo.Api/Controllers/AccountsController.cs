using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.responses.Accounts;
using SuperMemo.Application.DTOs.responses.Cards;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Accounts;
using SuperMemo.Application.Interfaces.Cards;

namespace SuperMemo.Api.Controllers;

[Authorize]
[Route("api/accounts")]
public class AccountsController(
    IAccountService accountService,
    ICardService cardService,
    Application.Interfaces.Auth.ICurrentUser currentUser) : BaseController
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> GetMyAccount(CancellationToken cancellationToken)
    {
        var result = await accountService.GetMyAccountAsync(currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("me/cards")]
    public async Task<ActionResult<ApiResponse<List<CardResponse>>>> GetMyCards(CancellationToken cancellationToken)
    {
        var accountResult = await accountService.GetMyAccountAsync(currentUser.Id, cancellationToken);
        if (!accountResult.Success || accountResult.Data == null)
            return NotFound(ApiResponse<List<CardResponse>>.ErrorResponse("Account not found."));
        var cardsResult = await cardService.ListByAccountIdAsync(accountResult.Data.Id, cancellationToken);
        return cardsResult.Success ? Ok(cardsResult) : BadRequest(cardsResult);
    }

    [HttpGet("by-number/{accountNumber}")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> GetByAccountNumber(string accountNumber, CancellationToken cancellationToken)
    {
        var result = await accountService.GetByAccountNumberAsync(accountNumber, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
