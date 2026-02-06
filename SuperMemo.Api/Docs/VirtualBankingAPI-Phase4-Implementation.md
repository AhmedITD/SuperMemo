# Virtual Banking API – Phase 4: Implementation Guide

**Stack:** .NET 8, ASP.NET Core, Entity Framework Core 8, PostgreSQL, FluentValidation, JWT Bearer.

---

## 1. Project Structure & Modules

### Top-level layout

```
SuperMemo/
├── SuperMemo.sln
├── global.json
├── SuperMemo.Api/                    # API layer (HTTP, controllers, middleware)
├── SuperMemo.Application/            # Use-cases, DTOs, interfaces, validators
├── SuperMemo.Domain/                 # Entities, enums, domain exceptions
└── SuperMemo.Infrastructure/         # Persistence, auth, audit, payroll runner
```

### Folder roles and feature mapping

| Folder | Purpose | Feature mapping |
|--------|---------|------------------|
| **SuperMemo.Api** | Controllers, `Program.cs`, JSON options, exception handler registration | All modules exposed via routes |
| **Api/Controllers** | One controller per area: `AuthController`, `KycController`, `AccountsController`, `TransactionsController`, `AdminController`, `CardsController`, `PayrollController`, `DocsController` | Auth, KYC, Account, Transactions, Admin, Cards, Payroll |
| **Api/Common** | `BaseController` (shared API base) | Shared |
| **Api/Json** | `UtcDateTimeConverter`, `NullableUtcDateTimeConverter` | Shared serialization |
| **Application** | Services (use-cases), DTOs (requests/responses), interfaces, validators, `ErrorCodes`, `DependencyInjection` | All features |
| **Application/Services** | `AuthService`, `KycService`, `AdminApprovalService`, `AccountService`, `CardService`, `TransactionService`, `PayrollJobService` | Per-feature use-cases |
| **Application/DTOs/requests** | `Auth/*`, `Kyc/*`, `Admin/*`, `Cards/*`, `Transactions/*`, `Payroll/*` | Request contracts |
| **Application/DTOs/responses** | `Common/ApiResponse`, `Auth/*`, `Kyc/*`, `Accounts/*`, `Cards/*`, `Transactions/*`, `Admin/*`, `Payroll/*` | Response contracts |
| **Application/Interfaces** | `IAuthService`, `IKycService`, `IAdminApprovalService`, `IAccountService`, `ICardService`, `ITransactionService`, `IPayrollJobService`, `IPayrollRunnerService`, `IAuditEventLogger`, `ISuperMemoDbContext` | Abstractions |
| **Application/Validators** | FluentValidation validators per request type | Validation |
| **Application/Common** | `ErrorCodes`, `DateTimeExtensions` | Shared |
| **Domain** | Entities, enums, `DomainException`, `IAuditable`, `ISoftDeletable` | Core model |
| **Domain/Entities** | `User`, `Account`, `Card`, `Transaction`, `IcDocument`, `PassportDocument`, `LivingIdentityDocument`, `PayrollJob`, `AuditLog`, `RefreshToken` | Data model |
| **Domain/Enums** | `UserRole`, `KycStatus`, `KybStatus`, `ApprovalStatus`, `AccountStatus`, `CardType`, `TransactionStatus`, `KycDocumentStatus`, `PayrollJobStatus` | Status/type enums |
| **Infrastructure/Data** | `SuperMemoDbContext`, `EntityConfigurations/*`, `Migrations`, `Interceptors/AuditLogInterceptor` | Persistence |
| **Infrastructure/Services** | `GlobalExceptionHandler`, `PasswordService`, `JwtTokenGenerator`, `RefreshTokenService`, `CurrentUser`, `AuditEventLogger`, `EfCoreAuditLogSink`, `PayrollRunnerService`, `PayrollRunnerHostedService` | Auth, audit, payroll |
| **Infrastructure/Options** | `PayrollOptions` | Config |

---

## 2. Database Migrations / Models

### Where definitions live

- **Entities:** `SuperMemo.Domain/Entities/*.cs` (e.g. `User`, `Account`, `Card`, `Transaction`, `IcDocument`, `PassportDocument`, `LivingIdentityDocument`, `PayrollJob`).
- **Configuration:** `SuperMemo.Infrastructure/Data/EntityConfigurations/*.cs` (table names, keys, indexes, FKs, precision).
- **Migrations:** `SuperMemo.Infrastructure/Data/Migrations/` (or `Infrastructure/Migrations/` if you moved them).

### Key constraints and indexes (from configurations)

- **Users:** PK `Id`, unique index on `Email`, required: `FirstName`, `LastName`, `Email`, `PasswordHash`, `Role`; enums for `KycStatus`, `KybStatus`, `ApprovalStatus`.
- **Accounts:** PK `Id`, unique `AccountNumber`, **unique `UserId`** (enforces one account per user); FK `UserId` → `Users.Id` (Restrict); `Balance` decimal(18,4); `Status` enum.
- **Cards:** PK `Id`, unique `Number`; FK `AccountId` → `Accounts.Id` (Restrict); `ScHashed` required; `Type`, `IsActive`, `IsExpired`, `IsEmployeeCard`.
- **Transactions:** PK `Id`; FK `FromAccountId` → `Accounts.Id` (Restrict); **unique index** on `(FromAccountId, IdempotencyKey)` with filter `IdempotencyKey IS NOT NULL AND IdempotencyKey != ''`; `Amount` decimal(18,4); `Status` enum.
- **KYC documents:** Each has PK `Id`, FK `UserId` → `Users.Id` (Restrict), `Status` (e.g. `KycDocumentStatus`), type-specific required columns.
- **PayrollJobs:** PK `Id`, FK `EmployeeUserId` → `Users.Id` (Restrict); `Amount` decimal(18,4); `Status`, `NextRunAt`.

### One account per user

- Enforced by **unique index on `Accounts.UserId`** in `AccountConfiguration` (e.g. `HasIndex(x => x.UserId).IsUnique()`).
- Account is created only when admin **approves** the user (in `AdminApprovalService.ApproveOrRejectUserAsync`).

### Example entity reference (Account)

```csharp
// Domain/Entities/Account.cs
public class Account : BaseEntity
{
    public int UserId { get; set; }
    public decimal Balance { get; set; }
    public required string Currency { get; set; }
    public AccountStatus Status { get; set; }
    public required string AccountNumber { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Card> Cards { get; set; }
    public ICollection<Transaction> OutgoingTransactions { get; set; }
}
```

---

## 3. Auth & Security Implementation

### Registration and login

- **Registration:** `AuthController.Register` → `IAuthService.Register(RegisterRequest)`. Service: validate email uniqueness, hash password with `IPasswordService.HashPassword` (BCrypt, work factor 12), create `User` (role Customer), create refresh token via `IRefreshTokenService`, return JWT + refresh token.
- **Login:** `AuthController.Login` → `IAuthService.Login(LoginRequest)`. Service: `ValidateUserCredentialsAsync` (email + `IPasswordService.VerifyPassword`), issue JWT + refresh token.
- **Refresh:** `AuthController.Refresh` → validate refresh token, revoke old, issue new JWT + refresh token.

### Password and JWT

- **Password hashing:** `Infrastructure/Services/Auth/PasswordService.cs` uses BCrypt; never log or return password/plain SC.
- **JWT:** `JwtTokenGenerator` builds token with claims (e.g. name, email, role, sub=userId); secret from config `JwtSettings:Secret`; validated in `Program.cs` with `AddJwtBearer` (ValidateIssuer, ValidateAudience, ValidateLifetime, IssuerSigningKey). Secret must be in config/key vault, not in code.

### Protecting endpoints

- **Auth middleware:** `app.UseAuthentication()` and `app.UseAuthorization()` in `Program.cs`; `[Authorize]` on controllers/actions that require login.
- **Role-based access:** `[Authorize(Policy = "Admin")]` on admin-only controllers (Admin, Cards, Payroll). Policy in `Program.cs`: `RequireClaim(ClaimTypes.Role, "Admin")`.
- **Current user:** `ICurrentUser` (from claims) injected into controllers/services; used to scope data (e.g. `from_account_id` must belong to `currentUser.Id`).

### Sensitive data

- Do **not** log: `PasswordHash`, `ScHashed`, raw card numbers, full KYC document content. Log only IDs, action names, and non-sensitive metadata in `IAuditEventLogger` and exception handler.

---

## 4. KYC Endpoints Implementation

### Routes and handlers

- `POST /api/kyc/ic` → `KycController.SubmitIcDocument` → `IKycService.SubmitIcDocumentAsync(request, currentUser.Id)`.
- `POST /api/kyc/passport` → `SubmitPassportDocumentAsync`.
- `POST /api/kyc/living-identity` → `SubmitLivingIdentityDocumentAsync`.
- `GET /api/kyc/status` → `GetStatusAsync(userId)`.

### Request validation

- FluentValidation validators (if present) on request DTOs; e.g. required fields: `IdentityCardNumber`, `FullName`, `MotherFullName`, `BirthDate`, `BirthLocation` for IC.
- Invalid requests return 400 with `VALIDATION_FAILED` and `errors` dictionary (from `InvalidModelStateResponseFactory` or `GlobalExceptionHandler` for `ValidationException`).

### Mapping and status

- **Submit IC:** Map `SubmitIcDocumentRequest` → new `IcDocument` (UserId from auth, Status = Pending). Save document; set `User.KycStatus = KycStatus.Pending`; save. Return document Id.
- **Admin verify/reject:** Admin endpoints `PUT /api/admin/kyc/ic/{documentId}/verify` with body `{ "status": "Verified" | "Rejected" }`. Service updates `doc.Status` and `doc.User.KycStatus` (Verified/Rejected/Pending) in one save.

### Example service flow (submit IC)

```csharp
// Pseudo-code aligned with KycService.SubmitIcDocumentAsync
var doc = new IcDocument {
    UserId = userId,
    IdentityCardNumber = request.IdentityCardNumber,
    FullName = request.FullName,
    MotherFullName = request.MotherFullName,
    BirthDate = request.BirthDate,
    BirthLocation = request.BirthLocation,
    Status = KycDocumentStatus.Pending
};
db.IcDocuments.Add(doc);
await db.SaveChangesAsync(ct);
var user = await db.Users.FindAsync(userId);
if (user != null) { user.KycStatus = KycStatus.Pending; await db.SaveChangesAsync(ct); }
return ApiResponse<int>.SuccessResponse(doc.Id);
```

---

## 5. Admin Approval Implementation

### Endpoints

- **List pending users:** `GET /api/admin/users?approvalStatus=pending_approval&pageNumber=1&pageSize=10` → `AdminApprovalService.ListUsersByApprovalStatusAsync`. Query `Users` where Role = Customer, optional filter by `ApprovalStatus`, include `Account`; project to `UserApprovalListItemResponse`; paginate.
- **Approve/reject:** `POST /api/admin/users/{userId}/approval` body `{ "approvalStatus": "Approved" | "Rejected" }` → `ApproveOrRejectUserAsync`.

### Atomicity

- In `ApproveOrRejectUserAsync`: load user with account; set `user.ApprovalStatus`; if Approved, create `Account` (if null) with Status Active and unique `AccountNumber`, or set existing account Status = Active; if Rejected and account exists, set account Status = Closed. All in one or two `SaveChangesAsync` calls (same DbContext transaction when single SaveChanges).
- **Set account status:** `PUT /api/admin/accounts/{accountId}/status` body `{ "status": "Active" | "Frozen" | "Closed" }` → update `account.Status` and save.

### Audit

- After approve/reject and after set account status: `IAuditEventLogger.LogAsync("User", userId, "UserApproved"|"UserRejected", ...)` and `LogAsync("Account", accountId, "AccountStatusChanged", ...)` so all changes are audited.

---

## 6. Cards Implementation

### Admin create card

- **Endpoint:** `POST /api/admin/cards` (body: `IssueCardRequest`: AccountId, Number, Type, ExpiryDate, SecurityCode, IsEmployeeCard) → `CardsController.IssueCard` → `ICardService.IssueCardAsync`.

### Validation

- Account must exist and (optionally) be active.
- Card `Number` must be unique (check `db.Cards.AnyAsync(c => c.Number == request.Number)`); if duplicate → 400 `CARD_NUMBER_EXISTS`.
- ExpiryDate in future (validator); SecurityCode length (e.g. 3–4) in validator.

### Service logic

- Hash security code: `ScHashed = passwordService.HashPassword(request.SecurityCode)` (never store plain).
- `IsExpired = request.ExpiryDate.Date <= DateTime.UtcNow.Date`.
- Insert `Card` (AccountId, Number, Type, ExpiryDate, ScHashed, IsActive=true, IsExpired, IsEmployeeCard); save.
- Audit: `LogAsync("Card", card.Id, "CardIssued", new { AccountId, Type, IsEmployeeCard })`.
- Response: mask number (e.g. `"****" + Number[^4..]`) in `CardResponse`; never return `ScHashed` or full number in list/detail.

### Revoke

- `PUT /api/admin/cards/{cardId}/revoke` → set `card.IsActive = false`; save; audit `CardRevoked`.

---

## 7. Transactions Implementation (Core)

### POST /api/transactions/transfer

- **Request:** Body: `FromAccountId`, `ToAccountNumber`, `Amount`, `Purpose` (optional), `IdempotencyKey` (optional in body). **Header:** `Idempotency-Key` (optional). Controller sets `request.IdempotencyKey ??= Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim()` so key can come from header or body.
- **Validation:** FluentValidation: FromAccountId > 0, ToAccountNumber non-empty, Amount > 0, IdempotencyKey non-empty (max 64). If missing → 400 VALIDATION_FAILED.

### Checks in service (in order)

1. **Auth/ownership:** Load account where `Id == request.FromAccountId && UserId == userId`. If null → 400 RESOURCE_NOT_FOUND.
2. **Account status:** `fromAccount.Status == AccountStatus.Active`; else → 400 ACCOUNT_INACTIVE.
3. **Amount:** `request.Amount > 0`.
4. **Idempotency:** If `request.IdempotencyKey` not empty, look up existing transaction by `(FromAccountId, IdempotencyKey)`. If found → return **200 + existing transaction** (idempotent replay).
5. **To account:** Load by `ToAccountNumber`; exists and Status == Active; else DESTINATION_ACCOUNT_NOT_FOUND or ACCOUNT_INACTIVE.
6. **Balance:** `fromAccount.Balance >= request.Amount`; else → 400 INSUFFICIENT_FUNDS.

### Status lifecycle and persistence

- Create `Transaction` with Status = Sending (or Created then Sending in same transaction).
- In the **same** unit of work: debit `fromAccount.Balance`, credit `toAccount.Balance`, set transaction Status = Completed.
- Single `SaveChangesAsync` so all or nothing (atomic).
- On success: audit `TransferCompleted` with transaction id and non-sensitive payload; then (if audit is separate write) second SaveChanges for audit row.

### Pseudo-code (transaction service)

```csharp
// CreateTransferAsync (simplified)
async Task<ApiResponse<TransactionResponse>> CreateTransferAsync(CreateTransferRequest request, int userId, CancellationToken ct)
{
    var fromAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Id == request.FromAccountId && a.UserId == userId, ct);
    if (fromAccount == null) return Error(ResourceNotFound);
    if (fromAccount.Status != Active) return Error(AccountInactive);
    if (request.Amount <= 0) return Error(validation);

    if (!string.IsNullOrWhiteSpace(request.IdempotencyKey)) {
        var existing = await db.Transactions.FirstOrDefaultAsync(
            t => t.FromAccountId == request.FromAccountId && t.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing != null) return Ok(Map(existing));  // idempotent replay
    }

    var toAccount = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccountNumber, ct);
    if (toAccount == null) return Error(DestinationAccountNotFound);
    if (toAccount.Status != Active) return Error(AccountInactive);
    if (fromAccount.Balance < request.Amount) return Error(InsufficientFunds);

    var txn = new Transaction { FromAccountId = request.FromAccountId, ToAccountNumber = request.ToAccountNumber,
        Amount = request.Amount, Status = Sending, Purpose = request.Purpose, IdempotencyKey = request.IdempotencyKey };
    db.Transactions.Add(txn);
    fromAccount.Balance -= request.Amount;
    toAccount.Balance += request.Amount;
    txn.Status = Completed;

    await db.SaveChangesAsync(ct);
    await auditLogger.LogAsync("Transaction", txn.Id.ToString(), "TransferCompleted", new { ... }, ct);
    await db.SaveChangesAsync(ct);
    return Ok(Map(txn));
}
```

### Error handling

- All business errors return `ApiResponse<T>.ErrorResponse(message, code: ErrorCodes.*)`. GlobalExceptionHandler maps `DomainException` to 400 and includes `Code` when provided.

---

## 8. Transaction History Implementation

### GET /api/transactions/account/{accountId}

- **Query:** Optional `status`, `pageNumber`, `pageSize`. Controller passes to `ITransactionService.ListByAccountAsync(accountId, status, pageNumber, pageSize)`.
- **Authorization:** Caller is authenticated; service should ensure the account belongs to the current user when used from a “my transactions” flow (e.g. only list for accounts owned by userId). In current design, client passes accountId and service returns list; ensure in API or service that accountId is one of the user’s (e.g. get “my account” first and use its id).
- **Implementation:** Query `Transactions` where `FromAccountId == accountId`; optional filter by `Status`; order by `CreatedAt` desc; skip/take for pagination; map to `TransactionResponse` list; return `PaginatedListResponse<TransactionResponse>`.

### GET /api/transactions/{id}

- **Implementation:** `GetByIdAsync(transactionId, userId)`. Load transaction where `Id == id && FromAccount.UserId == userId`. If not found → 404 RESOURCE_NOT_FOUND. Return single `TransactionResponse` (no sensitive data; idempotency key can be returned for debugging or omitted by policy).

---

## 9. Payroll / Salary Job Implementation

### Scheduled runner

- **Hosted service:** `PayrollRunnerHostedService` (BackgroundService) runs in a loop: wait (e.g. 1 hour), then create scope and resolve `IPayrollRunnerService`, call `RunDueJobsAsync`, log count processed or errors.
- **Configuration:** `Payroll:SourceAccountNumber` in appsettings. If empty or missing, runner does nothing (returns 0).

### RunDueJobsAsync (high-level)

- Get source account by `SourceAccountNumber`; if null, return 0.
- Load `PayrollJobs` where `Status == Active && NextRunAt <= UtcNow` (include EmployeeUser if needed).
- For each job:
  - Get employee’s account (by `UserId`).
  - Idempotency key: `payroll_{jobId}_{yyyyMM}` (e.g. from `job.NextRunAt`).
  - Call `ITransactionService.CreatePayrollCreditAsync(sourceAccount.Id, employeeAccount.AccountNumber, job.Amount, idempotencyKey, "Payroll {period}", ct)`. That method: no user-ownership check, no balance check on source account; same idempotency lookup and debit/credit as normal transfer.
  - If success: set `job.NextRunAt = (job.NextRunAt ?? now).AddMonths(1)`; save; audit `PayrollRun` with job id, employee id, period; increment processed count.
- Return processed count.

### Idempotency

- Same key `payroll_{jobId}_{yyyyMM}` for the same job and month causes `CreatePayrollCreditAsync` to find an existing transaction and return it without creating a second credit. So retries or double-run do not double-pay.

---

## 10. Error Handling & Response Format

### Common format

- Success: `{ "success": true, "data": { ... } }` (optional `message`).
- Error: `{ "success": false, "message": "...", "code": "ERROR_CODE", "errors": { "field": ["..."] } }` (errors only for validation).

### Mechanism

- **Validation:** FluentValidation runs before action; on failure, `InvalidModelStateResponseFactory` (or automatic BadRequest) returns 400 with message and optional `code: "VALIDATION_FAILED"` and `errors` from ModelState.
- **Exceptions:** `GlobalExceptionHandler` (IExceptionHandler) catches exceptions, maps to status code and optional `code`, returns same JSON shape. Map: `ValidationException` → 400, VALIDATION_FAILED; `DomainException` → 400, `ex.Code`; `UnauthorizedAccessException` → 401; `KeyNotFoundException` → 404, RESOURCE_NOT_FOUND; rest → 500 (message only in development).

### Example responses

- **USER_NOT_APPROVED** (if you throw DomainException with Code): `400 { "success": false, "message": "User is not approved.", "code": "USER_NOT_APPROVED" }`.
- **KYC_PENDING:** Same pattern with code `KYC_PENDING`.
- **ACCOUNT_INACTIVE:** 400, code `ACCOUNT_INACTIVE`.
- **INSUFFICIENT_FUNDS:** 400, code `INSUFFICIENT_FUNDS`.
- **IDEMPOTENT_REPLAY:** 200 with same transaction payload (no separate error code; client sees 200 and same data).
- **VALIDATION_FAILED:** 400, code `VALIDATION_FAILED`, `errors`: `{ "IdempotencyKey": ["Idempotency-Key is required (header or body)."] }`.

### Logging

- In exception handler: log exception and message; do **not** log request body (may contain PII/tokens). For 4xx log at Warning; for 5xx at Error.

---

## 11. Logging, Auditing & Observability

### Where to log

- **Service layer:** Log business outcomes (e.g. “Transfer completed”, “Payroll run processed N jobs”) at Information; log handled business errors (e.g. insufficient funds) at Warning.
- **Exception handler:** Log unhandled exceptions (Warning/Error) with message; no stack in production if sensitive.
- **Auth:** Log login success/failure (e.g. by email hash or id) at Information/Warning; do not log passwords or tokens.

### Audit events (explicit)

Use `IAuditEventLogger.LogAsync(entityType, entityId, action, changes)` for:

- **User registration:** Optional (or rely on EF audit for User insert).
- **KYC submission:** Optional per document type (e.g. “KycSubmitted”).
- **KYC status change:** `KycVerified` / `KycRejected` for IC, Passport, LivingIdentity (in AdminApprovalService).
- **Admin approval/reject:** `UserApproved`, `UserRejected`, `AccountStatusChanged`.
- **Card:** `CardIssued`, `CardRevoked`.
- **Transaction:** `TransferCompleted` (with transaction id; avoid logging full amount in free text if policy requires).
- **Payroll:** `PayrollRun` (job id, employee id, period).

All written to `AuditLog` table (UserId, EntityType, EntityId, Action, Changes JSON, Timestamp) in the same DbContext so they can be committed with the request or in a follow-up SaveChanges.

### Metrics (suggested)

- Request count and latency per route (middleware or filter).
- Transaction creation count (success/failure) per day.
- Payroll run count and duration; failed job count.
- Expose via ASP.NET Core metrics / OpenTelemetry if available, and connect to your monitoring (e.g. Prometheus, Application Insights).

---

*Document version: 1.0 – Phase 4 Implementation. Use with Phase 1 (Planning), Phase 2 (Requirements), and Phase 3 (System Design) for the full Virtual Banking API lifecycle.*
