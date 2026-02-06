namespace SuperMemo.Application.Common;

/// <summary>
/// Standard error codes for API responses (Phase 2 requirements).
/// </summary>
public static class ErrorCodes
{
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string UserNotApproved = "USER_NOT_APPROVED";
    public const string KycPending = "KYC_PENDING";
    public const string KycRejected = "KYC_REJECTED";
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string DestinationAccountNotFound = "DESTINATION_ACCOUNT_NOT_FOUND";
    public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
    public const string CardNumberExists = "CARD_NUMBER_EXISTS";
    public const string CardExpired = "CARD_EXPIRED";
    /// <summary>No active, non-expired card on the source account (required for transfers).</summary>
    public const string NoActiveCard = "NO_ACTIVE_CARD";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
}
