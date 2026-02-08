using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Cards;
using SuperMemo.Application.DTOs.responses.Cards;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Accounts;
using SuperMemo.Application.Interfaces.Cards;

namespace SuperMemo.Api.Controllers;

/// <summary>
/// Card operations for the current user (any authenticated user/customer). User role can list and create cards for their own account.
/// </summary>
[Authorize]
[Route("api/user/cards")]
public class UserCardsController(IAccountService accountService, ICardService cardService, Application.Interfaces.Auth.ICurrentUser currentUser) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CardResponse>>>> GetMyCards(CancellationToken cancellationToken)
    {
        var accountResult = await accountService.GetMyAccountAsync(currentUser.Id, cancellationToken);
        if (!accountResult.Success || accountResult.Data == null)
            return NotFound(ApiResponse<List<CardResponse>>.ErrorResponse("Account not found."));
        var cardsResult = await cardService.ListByAccountIdAsync(accountResult.Data.Id, cancellationToken);
        return cardsResult.Success ? Ok(cardsResult) : BadRequest(cardsResult);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CardIssuedResponse>>> CreateCard([FromBody] CreateMyCardRequest request, CancellationToken cancellationToken)
    {
        var accountResult = await accountService.GetMyAccountAsync(currentUser.Id, cancellationToken);
        if (!accountResult.Success || accountResult.Data == null)
            return NotFound(ApiResponse<CardIssuedResponse>.ErrorResponse("Account not found."));
        var result = await cardService.IssueCardForCurrentUserAsync(accountResult.Data.Id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
