namespace SuperMemo.Application.Common;

/// <summary>
/// Standard error codes for API responses (Phase 2 requirements).
/// </summary>
public static class ErrorCodes
{
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string PhoneAlreadyExists = "PHONE_ALREADY_EXISTS";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string UserNotApproved = "USER_NOT_APPROVED";
    public const string KycPending = "KYC_PENDING";
    public const string KycRejected = "KYC_REJECTED";
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string DestinationAccountNotFound = "DESTINATION_ACCOUNT_NOT_FOUND";
    public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
    public const string CardNumberExists = "CARD_NUMBER_EXISTS";
    public const string CardExpired = "CARD_EXPIRED";
    /// <summary>Account has reached the maximum number of active cards allowed.</summary>
    public const string MaxCardsExceeded = "MAX_CARDS_EXCEEDED";
    /// <summary>No active, non-expired card on the source account (required for transfers).</summary>
    public const string NoActiveCard = "NO_ACTIVE_CARD";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    /// <summary>OTP verification code invalid or expired.</summary>
    public const string InvalidVerificationCode = "INVALID_VERIFICATION_CODE";
    
    // Advanced features error codes
    public const string TemporaryFailure = "TEMPORARY_FAILURE";
    public const string PermanentFailure = "PERMANENT_FAILURE";
    public const string InvalidStatusTransition = "INVALID_STATUS_TRANSITION";
    public const string HighRiskTransaction = "HIGH_RISK_TRANSACTION";
    
    // Phase 8 error codes
    public const string DailyLimitExceeded = "DAILY_LIMIT_EXCEEDED";
    public const string InvalidAccountType = "INVALID_ACCOUNT_TYPE";
    public const string AccountTypeImmutable = "ACCOUNT_TYPE_IMMUTABLE";
    
    // Phase 9 - Payment Gateway error codes
    public const string PaymentInitiationFailed = "PAYMENT_INITIATION_FAILED";
    public const string PaymentVerificationFailed = "PAYMENT_VERIFICATION_FAILED";
    public const string PaymentCancellationFailed = "PAYMENT_CANCELLATION_FAILED";
    public const string InvalidWebhookSignature = "INVALID_WEBHOOK_SIGNATURE";
    public const string PaymentAlreadyProcessed = "PAYMENT_ALREADY_PROCESSED";
    public const string PaymentAmountMismatch = "PAYMENT_AMOUNT_MISMATCH";
    public const string InvalidOperation = "INVALID_OPERATION";
    public const string InternalError = "INTERNAL_ERROR";
}
