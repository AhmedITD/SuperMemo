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
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class CardService(ISuperMemoDbContext db, IPasswordService passwordService, IAuditEventLogger auditLogger) : ICardService
{
    /// <summary>Maximum number of active cards a single account can have (user-issued and admin-issued).</summary>
    private const int MaxActiveCardsPerAccount = 10;
    private static async Task<string> GenerateUniqueCardNumberAsync(ISuperMemoDbContext context, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var number = string.Create(16, Random.Shared, (span, random) =>
            {
                for (int i = 0; i < 16; i++)
                    span[i] = (char)('0' + (i == 0 ? random.Next(1, 10) : random.Next(0, 10)));
            });
            if (!await context.Cards.AnyAsync(c => c.Number == number, cancellationToken))
                return number;
        }
        throw new InvalidOperationException("Could not generate a unique card number.");
    }

    private static string GenerateSecurityCode()
    {
        return Random.Shared.Next(100, 1000).ToString(); // 3 digits
    }

    public async Task<ApiResponse<CardIssuedResponse>> IssueCardForCurrentUserAsync(int accountId, CreateMyCardRequest request, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts.FindAsync([accountId], cancellationToken);
        if (account == null)
            return ApiResponse<CardIssuedResponse>.ErrorResponse("Account not found.", code: ErrorCodes.ResourceNotFound);
        if (account.Status != AccountStatus.Active)
            return ApiResponse<CardIssuedResponse>.ErrorResponse("Only active accounts can create new cards.", code: ErrorCodes.AccountInactive);

        var activeCardCount = await db.Cards.CountAsync(c => c.AccountId == accountId && c.IsActive, cancellationToken);
        if (activeCardCount >= MaxActiveCardsPerAccount)
            return ApiResponse<CardIssuedResponse>.ErrorResponse(
                $"Maximum number of active cards ({MaxActiveCardsPerAccount}) reached for this account. Revoke an existing card before creating a new one.",
                code: ErrorCodes.MaxCardsExceeded);

        var number = await GenerateUniqueCardNumberAsync(db, cancellationToken);
        var expiryDate = DateTime.UtcNow.Date.AddYears(5);
        var securityCode = GenerateSecurityCode();
        var isExpired = expiryDate <= DateTime.UtcNow.Date;

        var card = new Card
        {
            AccountId = accountId,
            Number = number,
            Type = request.Type,
            ExpiryDate = expiryDate,
            ScHashed = passwordService.HashPassword(securityCode),
            IsActive = true,
            IsExpired = isExpired,
            IsEmployeeCard = false
        };
        db.Cards.Add(card);
        await db.SaveChangesAsync(cancellationToken); // persist card so Id is set
        await auditLogger.LogAsync("Card", card.Id.ToString(), "CardIssued", new { accountId, request.Type, IsEmployeeCard = false }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken); // persist audit log

        var masked = number.Length > 4 ? "****" + number[^4..] : "****";
        var response = new CardIssuedResponse
        {
            Id = card.Id,
            AccountId = card.AccountId,
            NumberMasked = masked,
            Type = card.Type,
            ExpiryDate = card.ExpiryDate,
            IsActive = card.IsActive,
            IsExpired = card.IsExpired,
            IsEmployeeCard = card.IsEmployeeCard,
            CreatedAt = card.CreatedAt,
            SecurityCode = securityCode
        };
        return ApiResponse<CardIssuedResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<CardResponse>> IssueCardAsync(IssueCardRequest request, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts.FindAsync([request.AccountId], cancellationToken);
        if (account == null)
            return ApiResponse<CardResponse>.ErrorResponse("Account not found.", code: ErrorCodes.ResourceNotFound);
        if (account.Status != AccountStatus.Active)
            return ApiResponse<CardResponse>.ErrorResponse("Account must be active to issue a card.", code: ErrorCodes.AccountInactive);

        if (await db.Cards.AnyAsync(c => c.Number == request.Number, cancellationToken))
            return ApiResponse<CardResponse>.ErrorResponse("Card number already exists.", code: ErrorCodes.CardNumberExists);

        if (request.ExpiryDate.Date <= DateTime.UtcNow.Date)
            return ApiResponse<CardResponse>.ErrorResponse("Expiry date must be in the future.", code: ErrorCodes.ValidationFailed);

        var activeCardCount = await db.Cards.CountAsync(c => c.AccountId == request.AccountId && c.IsActive, cancellationToken);
        if (activeCardCount >= MaxActiveCardsPerAccount)
            return ApiResponse<CardResponse>.ErrorResponse(
                $"Account has reached the maximum of {MaxActiveCardsPerAccount} active cards.",
                code: ErrorCodes.MaxCardsExceeded);

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
        await db.SaveChangesAsync(cancellationToken); // persist card so Id is set
        await auditLogger.LogAsync("Card", card.Id.ToString(), "CardIssued", new { request.AccountId, request.Type, request.IsEmployeeCard }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken); // persist audit log

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
        if (cardId <= 0)
            return ApiResponse<object>.ErrorResponse("Invalid card ID.", code: ErrorCodes.ValidationFailed);

        var card = await db.Cards.FindAsync([cardId], cancellationToken);
        if (card == null)
            return ApiResponse<object>.ErrorResponse("Card not found.", code: ErrorCodes.ResourceNotFound);
        if (!card.IsActive)
            return ApiResponse<object>.ErrorResponse("Card is already revoked.", code: ErrorCodes.InvalidOperation);

        card.IsActive = false;
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
