using SuperMemo.Application.DTOs.requests.Cards;
using SuperMemo.Application.DTOs.responses.Cards;
using SuperMemo.Application.DTOs.responses.Common;

namespace SuperMemo.Application.Interfaces.Cards;

public interface ICardService
{
    Task<ApiResponse<CardResponse>> IssueCardAsync(IssueCardRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<CardResponse>>> ListByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> RevokeCardAsync(int cardId, CancellationToken cancellationToken = default);
}
