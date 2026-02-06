using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.requests.Cards;
using SuperMemo.Application.DTOs.responses.Cards;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Application.Interfaces.Cards;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Application.Services;

public class CardService(ISuperMemoDbContext db, IPasswordService passwordService, IAuditEventLogger auditLogger) : ICardService
{
    public async Task<ApiResponse<CardResponse>> IssueCardAsync(IssueCardRequest request, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts.FindAsync([request.AccountId], cancellationToken);
        if (account == null)
            return ApiResponse<CardResponse>.ErrorResponse("Account not found.");

        if (await db.Cards.AnyAsync(c => c.Number == request.Number, cancellationToken))
            return ApiResponse<CardResponse>.ErrorResponse("Card number already exists.", code: ErrorCodes.CardNumberExists);

        var isExpired = request.ExpiryDate.Date <= DateTime.UtcNow.Date;
        var card = new Card
        {
            AccountId = request.AccountId,
            Number = request.Number,
            Type = request.Type,
            ExpiryDate = request.ExpiryDate,
            ScHashed = passwordService.HashPassword(request.SecurityCode),
            IsActive = true,
            IsExpired = isExpired,
            IsEmployeeCard = request.IsEmployeeCard
        };
        db.Cards.Add(card);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Card", card.Id.ToString(), "CardIssued", new { request.AccountId, request.Type, request.IsEmployeeCard }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ApiResponse<CardResponse>.SuccessResponse(Map(card));
    }

    public async Task<ApiResponse<List<CardResponse>>> ListByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var cards = await db.Cards
            .Where(c => c.AccountId == accountId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
        return ApiResponse<List<CardResponse>>.SuccessResponse(cards.Select(Map).ToList());
    }

    public async Task<ApiResponse<object>> RevokeCardAsync(int cardId, CancellationToken cancellationToken = default)
    {
        var card = await db.Cards.FindAsync([cardId], cancellationToken);
        if (card == null)
            return ApiResponse<object>.ErrorResponse("Card not found.");

        card.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Card", cardId.ToString(), "CardRevoked", null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.SuccessResponse(new { });
    }

    private static CardResponse Map(Card c)
    {
        var masked = c.Number.Length > 4 ? "****" + c.Number[^4..] : "****";
        return new CardResponse
        {
            Id = c.Id,
            AccountId = c.AccountId,
            NumberMasked = masked,
            Type = c.Type,
            ExpiryDate = c.ExpiryDate,
            IsActive = c.IsActive,
            IsExpired = c.IsExpired,
            IsEmployeeCard = c.IsEmployeeCard,
            CreatedAt = c.CreatedAt
        };
    }
}
