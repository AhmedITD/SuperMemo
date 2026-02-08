# SuperMemo Database Schema

## ER Diagram (Mermaid)

View this file in GitHub, VS Code (with Mermaid extension), or [Mermaid Live Editor](https://mermaid.live).

```mermaid
erDiagram
    Users ||--o| Accounts : "has one"
    Users ||--o{ IcDocuments : "has"
    Users ||--o{ PassportDocuments : "has"
    Users ||--o{ LivingIdentityDocuments : "has"
    Users ||--o{ RefreshTokens : "has"
    Users ||--o{ Payments : "has"
    Users ||--o{ PayrollJobs : "employee"

    Accounts ||--o{ Cards : "has"
    Accounts ||--o{ Transactions : "from"
    Accounts ||--o{ Payments : "has"
    Accounts ||--o| MerchantAccounts : "has"

    Transactions }o--o| Payments : "linked"
    Transactions ||--o{ TransactionStatusHistory : "has"

    Payments ||--o{ PaymentWebhookLogs : "has"

    Users {
        int Id PK
        string FullName
        string Phone UK
        string PasswordHash
        int Role
        string ImageUrl
        int KycStatus
        int KybStatus
        int ApprovalStatus
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    Accounts {
        int Id PK
        int UserId FK
        decimal Balance
        string Currency
        int Status
        string AccountNumber UK
        int AccountType
        decimal DailySpendingLimit
        decimal DailySpentAmount
        datetime LastInterestCalculationDate
        datetime LastDailyLimitResetDate
        datetime CreatedAt
        datetime UpdatedAt
    }

    Cards {
        int Id PK
        int AccountId FK
        string Number UK
        int Type
        datetime ExpiryDate
        string ScHashed
        bool IsActive
        bool IsExpired
        bool IsEmployeeCard
        datetime CreatedAt
        datetime UpdatedAt
    }

    Transactions {
        int Id PK
        int FromAccountId FK
        string ToAccountNumber
        decimal Amount
        int TransactionType
        int Status
        string Purpose
        string IdempotencyKey
        int Category
        int PaymentId FK
        int RetryCount
        int RiskScore
        int RiskLevel
        datetime StatusChangedAt
        datetime CreatedAt
        datetime UpdatedAt
    }

    Payments {
        int Id PK
        int UserId FK
        int AccountId FK
        string PaymentGateway
        string GatewayPaymentId
        string RequestId
        decimal Amount
        string Currency
        int Status
        string PaymentUrl
        int TransactionId FK
        bool WebhookReceived
        string WebhookData
        datetime CreatedAt
        datetime UpdatedAt
    }

    PaymentWebhookLogs {
        int Id PK
        int PaymentId FK
        text WebhookPayload
        string Signature
        bool SignatureValid
        bool Processed
        datetime ProcessedAt
        string ErrorMessage
        datetime CreatedAt
        datetime UpdatedAt
    }

    RefreshTokens {
        uuid Id PK
        string TokenHash
        int UserId FK
        datetime ExpiresAt
        datetime CreatedAt
        datetime RevokedAt
    }

    AuditLogs {
        uuid Id PK
        int UserId FK
        string EntityType
        string EntityId
        string Action
        text Changes
        datetime Timestamp
    }

    IcDocuments {
        int Id PK
        int UserId FK
        string IdentityCardNumber
        string FullName
        string MotherFullName
        datetime BirthDate
        string BirthLocation
        string ImageUrl
        int Status
        datetime CreatedAt
        datetime UpdatedAt
    }

    PassportDocuments {
        int Id PK
        int UserId FK
        string PassportNumber
        string FullName
        string Nationality
        datetime BirthDate
        datetime ExpiryDate
        string ImageUrl
        int Status
        datetime CreatedAt
        datetime UpdatedAt
    }

    LivingIdentityDocuments {
        int Id PK
        int UserId FK
        string SerialNumber
        string FullFamilyName
        string LivingLocation
        string FormNumber
        string ImageUrl
        int Status
        datetime CreatedAt
        datetime UpdatedAt
    }

    PayrollJobs {
        int Id PK
        int EmployeeUserId FK
        string EmployerId
        decimal Amount
        string Currency
        string Schedule
        datetime NextRunAt
        int Status
        datetime CreatedAt
        datetime UpdatedAt
    }

    PhoneVerificationCodes {
        int Id PK
        string PhoneNumber
        string Code
        datetime ExpiresAt
        datetime VerifiedAt
        bool IsUsed
        string IpAddress
        datetime CreatedAt
        datetime UpdatedAt
    }

    MerchantAccounts {
        int Id PK
        int AccountId FK
        string MerchantId
        string MerchantName
        text QrCodeData
        string NfcUrl
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }

    FraudDetectionRules {
        int Id PK
        string RuleName
        string RuleType
        decimal ThresholdValue
        int ThresholdCount
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }

    TransactionStatusHistory {
        int Id PK
        int TransactionId FK
        int OldStatus
        int NewStatus
        datetime ChangedAt
        int ChangedBy
        string Reason
        datetime CreatedAt
        datetime UpdatedAt
    }
```

## How to view the diagram

1. **dbdiagram.io**  
   - Open [https://dbdiagram.io](https://dbdiagram.io).  
   - Copy the contents of `database-schema.dbml` and paste into the editor to see the schema and export as image/PDF.

2. **Mermaid (this file)**  
   - Open this `.md` file in GitHub, or in VS Code with a Mermaid extension, or paste the `mermaid` block into [Mermaid Live Editor](https://mermaid.live).

3. **Tables summary**  
   - **Users** – app users (Admin/Customer), KYC/approval state.  
   - **Accounts** – one per user, balance, currency, status, account number.  
   - **Cards** – cards linked to an account.  
   - **Transactions** – transfers, top-ups, interest; links to FromAccount and optional Payment.  
   - **Payments** – gateway payments (e.g. QiCard); link to User, Account, and optional Transaction.  
   - **PaymentWebhookLogs** – webhook payloads and processing status.  
   - **RefreshTokens** – JWT refresh tokens per user.  
   - **AuditLogs** – audit trail (entity, action, changes).  
   - **IcDocuments, PassportDocuments, LivingIdentityDocuments** – KYC documents per user.  
   - **PayrollJobs** – payroll runs (employee user, amount, schedule).  
   - **PhoneVerificationCodes** – OTP codes for phone verification.  
   - **MerchantAccounts** – merchant/QR per account.  
   - **FraudDetectionRules** – configurable fraud rules.  
   - **TransactionStatusHistory** – status change history for transactions.
