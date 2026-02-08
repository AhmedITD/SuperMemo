using SuperMemo.Application.DTOs.requests.Cards;
using SuperMemo.Application.DTOs.responses.Cards;
using SuperMemo.Application.DTOs.responses.Common;

namespace SuperMemo.Application.Interfaces.Cards;

public interface ICardService
{
    Task<ApiResponse<CardResponse>> IssueCardAsync(IssueCardRequest request, CancellationToken cancellationToken = default);
    /// <summary>Issue a new card for the given account (current user's account). Generates number, expiry, and security code. Returns the card with one-time security code.</summary>
    Task<ApiResponse<CardIssuedResponse>> IssueCardForCurrentUserAsync(int accountId, CreateMyCardRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<CardResponse>>> ListByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> RevokeCardAsync(int cardId, CancellationToken cancellationToken = default);
}
