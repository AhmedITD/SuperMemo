using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Cards;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Cards;

namespace SuperMemo.Api.Controllers;

[Authorize(Policy = "Admin")]
[Route("api/admin/cards")]
public class CardsController(ICardService cardService) : BaseController
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Cards.CardResponse>>> IssueCard([FromBody] IssueCardRequest request, CancellationToken cancellationToken)
    {
        var result = await cardService.IssueCardAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("account/{accountId}")]
    public async Task<ActionResult<ApiResponse<List<Application.DTOs.responses.Cards.CardResponse>>>> ListByAccount(int accountId, CancellationToken cancellationToken)
    {
        var result = await cardService.ListByAccountIdAsync(accountId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{cardId}/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeCard(int cardId, CancellationToken cancellationToken)
    {
        var result = await cardService.RevokeCardAsync(cardId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
