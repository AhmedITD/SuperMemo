# Virtual Banking API – Phase 3: System Design

---

## 1. Overall Architecture

### High-level architecture
- **Layered monolith** with clear separation:
  - **API layer** (ASP.NET Core controllers): HTTP handling, route mapping, request/response DTOs, validation (FluentValidation), authorization (JWT + roles).
  - **Application / use-case layer** (services): orchestration, business rules, validation gates (KYC, approval, account/card status), calls to persistence.
  - **Domain layer**: entities, enums, domain exceptions.
  - **Infrastructure / persistence layer**: EF Core DbContext, repositories (if used), audit sink, external concerns (hashing, JWT).

- **Modular by feature** within the monolith: Auth, KYC, Admin Approval, Accounts, Cards, Transactions, Payroll. Each has its own DTOs, service interface(s), and controller(s). Shared cross-cutting: audit, idempotency, error codes.

### Main components / modules
| Module | Responsibility | Owns |
|--------|----------------|------|
| **Auth** | Register, login, refresh, /me | User identity, password hash, JWT, refresh tokens |
| **KYC** | Submit IC/Passport/LivingIdentity, get status; admin verify | KYC documents, user kyc_status |
| **Admin Approval** | List users by status, approve/reject user, set account status, verify KYC | User approval_status, account status |
| **Accounts** | One account per user, balance, currency, status | Account CRUD, get my account / by number |
| **Cards** | Issue (admin), list, revoke; mask number, hash security code | Card CRUD, sc_hashed, is_active/is_expired |
| **Transactions** | Create transfer, list, get by id; idempotency; state machine | Transaction lifecycle, balance updates |
| **Payroll** | CRUD PayrollJobs; run due jobs (scheduler) → create salary transactions | PayrollJob, salary credits |

- **Validation**: API layer (FluentValidation for DTOs); business rules in application services (e.g. “only active account can send”).
- **Persistence**: Infrastructure via `ISuperMemoDbContext`; services do not reference EF directly in interfaces.

### Technology stack (current)
- **Runtime**: .NET 8 (C#).
- **API**: ASP.NET Core (Minimal API or MVC controllers), JWT Bearer, FluentValidation.
- **Persistence**: PostgreSQL, Entity Framework Core 8, Npgsql.
- **Security**: BCrypt for passwords and card security code hash; JWT for auth.
- **Justification**: Strong typing, good EF support, built-in DI and middleware; PostgreSQL for ACID and indexing (idempotency, unique card number).

---

## 2. Detailed Data Model / ERD

### Entities and relationships (textual ERD)
- **Users** (1) —— (0..1) **Accounts**  
  One user has at most one account (created on approval). FK: `Accounts.user_id` → `Users.id` (unique).
- **Users** (1) —— (0..N) **IcDocuments**, **PassportDocuments**, **LivingIdentityDocuments**  
  One user can have multiple KYC documents over time; one document type per submission. FK: `*_Documents.user_id` → `Users.id`.
- **Accounts** (1) —— (0..N) **Cards**  
  One account has many cards. FK: `Cards.account_id` → `Accounts.id`.
- **Accounts** (1) —— (0..N) **Transactions** (outgoing)  
  Transactions reference `from_account_id` → `Accounts.id`; `to_account_number` is stored as string (lookup to account by number).
- **Users** (1) —— (0..N) **PayrollJobs** (as employee)  
  FK: `PayrollJobs.employee_user_id` → `Users.id`.

### Key attributes and PKs/FKs
- **Users**: id (PK), email (unique), phone (nullable), role, kyc_status, kyb_status, approval_status, created_at, updated_at.
- **KYC docs**: id (PK), user_id (FK), type-specific fields, status, created_at, updated_at.
- **Accounts**: id (PK), user_id (FK, unique), account_number (unique), balance, currency, status, created_at, updated_at.
- **Cards**: id (PK), account_id (FK), number (unique), type, expiry_date, sc_hashed, is_active, is_expired, is_employee_card, created_at.
- **Transactions**: id (PK), from_account_id (FK), to_account_number, amount, status, purpose, idempotency_key, created_at.
- **PayrollJobs**: id (PK), employee_user_id (FK), employer_id, amount, currency, schedule, next_run_at, status, created_at, updated_at.

### Important indexes
- `Users.email` (unique).
- `Accounts.user_id` (unique), `Accounts.account_number` (unique).
- `Cards.number` (unique), `Cards.account_id`.
- `Transactions.(from_account_id, idempotency_key)` (unique, partial: where idempotency_key is not null/empty).
- `Transactions.from_account_id`, `Transactions.created_at` (for list/filters).

### Enforcing constraints
- **One account per user**: Unique index on `Accounts.user_id`; account created only on admin approval.
- **KYC tied to user**: FK on each document table; queries always scoped by user_id.
- **Referential integrity for transactions**: `from_account_id` FK with `Restrict` (no cascade delete); closed accounts remain in DB; business rules prevent new debits/credits to non-active accounts.

---

## 3. Database Design & Constraints

### Per-table constraints and indexes
- **Users**: PK id; unique index on email; not null on email, password_hash, role, kyc_status, kyb_status, approval_status.
- **KYC docs**: PK id; FK user_id (restrict); not null on type-specific required fields and status.
- **Accounts**: PK id; unique user_id; unique account_number; check or enum for status (pending_approval | active | frozen | closed); balance ≥ 0 if desired (optional check).
- **Cards**: PK id; unique number; FK account_id (restrict); not null on number, type, expiry_date, sc_hashed.
- **Transactions**: PK id; FK from_account_id (restrict); unique (from_account_id, idempotency_key) with filter; not null on amount, status, to_account_number; check amount > 0.
- **PayrollJobs**: PK id; FK employee_user_id (restrict); status enum (active | paused | canceled).

### Idempotency at DB level
- **Unique index** on `(from_account_id, idempotency_key)` with filter so only non-null/non-empty keys are unique. Prevents two completed transactions with the same key for the same account.
- **Race handling**: First request inserts the row (status e.g. Sending) and updates balances in the same transaction; second request with same key does a lookup first and returns existing row (no insert), so duplicate key violation is avoided by “lookup then return” in application code.

### Monetary precision
- **Type**: `decimal(18,4)` for `balance` and `amount` (EF: `HasPrecision(18, 4)`). All arithmetic in decimal; avoid float/double to prevent rounding errors.

---

## 4. Service / Use-Case Design

| Use case | Inputs | Main steps | Output / errors |
|----------|--------|------------|------------------|
| **RegisterUser** | Email, password, name, phone? | Validate; check email unique; hash password; create User (role Customer); create refresh token; return JWT + refresh | Success: 201 + tokens. Error: EMAIL_ALREADY_EXISTS, validation |
| **LoginUser** | Email, password | Validate credentials; issue JWT + refresh | Success: 200 + tokens. Error: INVALID_CREDENTIALS |
| **SubmitKycIc** | UserId, IC fields | Validate fields; insert IcDocument (pending); set user KycStatus = Pending | Success: doc id. Error: validation |
| **SubmitKycPassport** | UserId, Passport fields | Same pattern for PassportDocument | Same |
| **SubmitLivingIdentity** | UserId, LivingIdentity fields | Same for LivingIdentityDocument | Same |
| **GetKycStatus** | UserId | Load user; find latest IC/Passport/LivingIdentity; return kyc/kyb status + document type + status | Success: KycStatusResponse |
| **ApproveUser** | UserId, ApprovalStatus | Load user (+ account); if Approved: create account if missing, set active; set user approval_status | Success. Error: user not found |
| **RejectUser** | UserId | Set approval_status Rejected; optionally set account status Closed | Success |
| **SetAccountStatus** | AccountId, Status | Update account.status (active/frozen/closed) | Success. Error: account not found |
| **GetAccountSummary** | UserId | Get account by user_id; return balance, currency, status, account_number | Success or RESOURCE_NOT_FOUND |
| **ListCards** | AccountId (or UserId → account) | List cards for account; mask number | Success: list CardResponse |
| **IssueCard** | AccountId, number, type, expiry, securityCode, is_employee_card | Validate account; check number unique; hash securityCode; insert Card; set is_expired from expiry_date | Success: CardResponse. Error: CARD_NUMBER_EXISTS, RESOURCE_NOT_FOUND |
| **RevokeCard** | CardId | Set is_active = false | Success. Error: not found |
| **CreateTransaction** | FromAccountId, ToAccountNumber, Amount, Purpose?, IdempotencyKey, UserId | Validate ownership; check account active; check amount > 0; **idempotency lookup** → if found return existing; validate to_account exists and active; check balance; insert Transaction (Sending); debit from_account, credit to_account; set status Completed; save | Success: TransactionResponse. Errors: INSUFFICIENT_FUNDS, ACCOUNT_INACTIVE, DESTINATION_ACCOUNT_NOT_FOUND, RESOURCE_NOT_FOUND |
| **GetTransaction** | TransactionId, UserId | Load transaction where FromAccount.UserId = UserId; return or 404 | Success: TransactionResponse |
| **ListTransactions** | AccountId, Status?, Page, PageSize | Filter by account (and status); order by created_at desc; page | Success: PaginatedListResponse |
| **RunPayrollJobs** | — | Select PayrollJobs where status=active and next_run_at ≤ now; for each: get employee account; create credit transaction (idempotent per job+period); set next_run_at (e.g. +1 month); log audit | Success: count processed. Errors: log and continue per job |

**Where checks are done**
- **KYC / approval**: Before allowing transfers, services check user approval_status and account status (and optionally kyc_status) in CreateTransaction and GetAccountSummary.
- **Account/card status**: CreateTransaction checks from and to account status; card is_active/is_expired used for display and payroll linkage, not for blocking transfer in MVP.
- **Transaction limits**: Can be added in CreateTransaction (max amount, daily cap) as product defines.
- **is_employee_card**: Used for reporting/payroll association; payroll run uses employee_user_id → account, not card.

---

## 5. API Layer Design

### Flow (all endpoints)
1. **HTTP request** → routing to controller.
2. **Auth** (where required): JWT validated; user/role from claims.
3. **Validation**: Model binding + FluentValidation; if invalid → 400 + VALIDATION_FAILED + errors.
4. **Service call**: Controller passes DTO + current user id (and idempotency key from header if applicable).
5. **Persistence**: Service uses DbContext; single transaction for write operations.
6. **Response**: Success → 200/201 + body; business error → 400/404 + code + message.

### Endpoint groups and DTOs (design level)

- **Auth**  
  - POST `/auth/register`: RegisterRequest (firstName, lastName, email, password [, phone]). Response: RegisterResponse (id, token, refreshToken, expires).  
  - POST `/auth/login`: LoginRequest (email, password). Response: LoginResponse (token, refreshToken, expires).  
  - GET `/auth/Me`: No body. Response: User (id, name, email, phone, role, kyc_status, kyb_status, approval_status).

- **KYC**  
  - POST `/api/kyc/ic`, `/api/kyc/passport`, `/api/kyc/living-identity`: Type-specific request DTOs. Response: { id }.  
  - GET `/api/kyc/status`: Response: KycStatusResponse (kycStatus, kybStatus, documentType, documentStatus).

- **Account**  
  - GET `/api/accounts/me`: Response: AccountResponse (id, userId, accountNumber, balance, currency, status, createdAt).  
  - GET `/api/accounts/me/cards`: Response: List of CardResponse (masked number, type, expiry, is_active, is_expired, is_employee_card).  
  - GET `/api/accounts/by-number/{accountNumber}`: Response: AccountResponse.

- **Transactions**  
  - POST `/api/transactions/transfer`: CreateTransferRequest (fromAccountId, toAccountNumber, amount, purpose?). **Idempotency-Key** in header (required) or body. Response: TransactionResponse.  
  - GET `/api/transactions/account/{accountId}`: Query: status, pageNumber, pageSize. Response: PaginatedListResponse&lt;TransactionResponse&gt;.  
  - GET `/api/transactions/{id}`: Response: TransactionResponse.

- **Admin**  
  - GET `/api/admin/users`: Query: approvalStatus?, pageNumber, pageSize. Response: PaginatedListResponse&lt;UserApprovalListItemResponse&gt;.  
  - POST `/api/admin/users/{userId}/approval`: Body: { approvalStatus }. Response: 200.  
  - PUT `/api/admin/accounts/{accountId}/status`: Body: { status }. Response: 200.  
  - PUT `/api/admin/kyc/ic|passport|living-identity/{documentId}/verify`: Body: { status }. Response: 200.  
  - POST `/api/admin/cards`: IssueCardRequest. Response: CardResponse.  
  - GET `/api/admin/cards/account/{accountId}`: Response: List CardResponse.  
  - PUT `/api/admin/cards/{cardId}/revoke`: Response: 200.

- **Payroll**  
  - GET `/api/admin/payroll`: Query: pageNumber, pageSize. Response: PaginatedListResponse&lt;PayrollJobResponse&gt;.  
  - POST `/api/admin/payroll`: CreatePayrollJobRequest. Response: PayrollJobResponse.  
  - GET `/api/admin/payroll/{jobId}`: Response: PayrollJobResponse.  
  - PUT `/api/admin/payroll/{jobId}`: UpdatePayrollJobRequest. Response: PayrollJobResponse.  
  - DELETE `/api/admin/payroll/{jobId}`: Response: 200.

### Idempotency-Key in POST /transactions
- **Source**: Prefer **header** `Idempotency-Key`; fallback to request body field `idempotencyKey` if header missing.
- **Validation**: Required for POST transfer; length ≤ 64; non-empty. If missing/invalid → 400 VALIDATION_FAILED.
- **Usage**: Service receives key; looks up existing transaction by (from_account_id, idempotency_key); if found, return same response (200 + existing transaction); otherwise create new and store key.

---

## 6. Transaction State Machine & Idempotency Design

### State machine for Transaction.status
- **States**: `Created` → `Sending` → `Completed` | `Failed`.
- **Transitions**:
  - **Created**: Initial state when row is inserted (optional; current implementation may go straight to Sending).
  - **Sending**: Set when balance update is about to be applied (or at insert if done in one transaction).
  - **Completed**: Set after debit/credit committed successfully.
  - **Failed**: Set if an exception occurs after insert (e.g. constraint failure); or by a retry/timeout process if implemented.
- **Component**: Application service (e.g. TransactionService) is the only place that changes status; no backward transitions (Completed/Failed are terminal).
- **Failures**: Log at service layer; persist Failed status if transaction row was already inserted; do not double-debit/credit.

### Idempotency handling
- **Storage**: Transaction row stores `idempotency_key` + `from_account_id`; unique index enforces at most one row per (from_account_id, idempotency_key).
- **Lookup**: Before creating a new transaction, select by (from_account_id, idempotency_key). If found → return that transaction’s response (same id, status, amount, etc.).
- **Second request while first is “sending”**: If both requests run concurrently, one will insert and the other will either (a) see the row after commit and return it, or (b) hit unique constraint; on constraint, re-query and return existing row. Client always receives 200 with same payload for same key.
- **Response**: Client receives 200 and the same TransactionResponse (id, status, amount, created_at) on replay.

---

## 7. Security & Compliance Design

### Authentication and authorization
- **JWT**: Issued on login/register/refresh; validated on protected routes; claims include user id and role (Customer | Admin).
- **Authorization**: Role-based; admin-only routes use policy `Authorize(Policy = "Admin")` (require role Admin). Customer can access only own resources (account, transactions by own account, KYC status).

### Password and card data
- **Passwords**: BCrypt (work factor ≥ 12); never logged or returned.
- **Card number**: Stored in full for lookups; **masked in all API responses and logs** (e.g. ****1234).
- **Security code (SC)**: Never stored raw; only **sc_hashed** (BCrypt) in DB; never returned or logged.

### KYC document protection
- KYC document content stored in DB with access only via application layer; admin verify endpoints only change status. No raw documents in logs; audit log records “KYC verified/rejected” and document id, not PII fields.

### Audit events (where to log)
- **User registration**: Log entity User, action Added (or “UserRegistered”), user id.
- **KYC submissions**: Log IcDocument/PassportDocument/LivingIdentityDocument Added; optional “KycSubmitted” with document type.
- **KYC status changes**: Log when admin verifies/rejects (document Modified or explicit “KycVerified”/“KycRejected”).
- **Admin approvals/rejections**: Log User/Account Modified or “UserApproved”/“UserRejected”, “AccountStatusChanged”.
- **Card issuance and status changes**: Log Card Added, Card Modified (revoke = is_active false).
- **Transactions**: Log Transaction Added with action “TransferCreated” or “TransferCompleted” (no amount in log if sensitive; or hash).
- **Payroll runs**: Log “PayrollRun” with job id, employee id, period, outcome (success/fail).

Implementation: **explicit audit logging** in the application layer (e.g. IAuditEventLogger) for these events, in addition to any EF change-tracking audit for IAuditable entities.

### Rate limiting / throttling
- **Login**: Throttle by IP or email (e.g. 5/minute) to reduce brute force.
- **KYC submission**: Limit per user (e.g. 5/hour) to avoid abuse.
- **Transactions**: Limit per account or user (e.g. 100/minute) to reduce impact of bugs or abuse.
- Implement via middleware or gateway (e.g. AspNetCoreRateLimit, or API gateway).

---

## 8. Payroll / Salary Engine Design

### Triggering
- **Scheduler**: Background hosted service (e.g. IHostedService) or cron job that runs periodically (e.g. every hour or daily). Alternatively, an admin-only endpoint “Run payroll now” for testing.
- **Selection**: Query PayrollJobs where `status = active` and `next_run_at <= UtcNow` (and optionally employer/tenant filter). Order by next_run_at.

### Creating salary transactions safely
- For each due job:
  - Resolve employee’s account (user → account); skip if no account or account not active.
  - **Idempotency**: Use a deterministic key per job and period (e.g. `payroll_{jobId}_{yyyyMM}`) so the same job+month cannot create two credits.
  - In a single DB transaction: insert Transaction (credit to account), update account balance, update PayrollJob.next_run_at (e.g. add one month).
- **Double-pay prevention**: Unique constraint on (idempotency_key, from_account_id) does not apply to credits from “system”; instead use a dedicated idempotency key per payroll job+period and check for existing transaction with that key before creating. If exists, skip and still advance next_run_at.

### Reconciliation
- Store payroll run id or (job_id, period) in a small table or in Transaction (e.g. purpose = “Payroll {jobId} {period}”) for traceability. Audit log entry per run and per job processed.

### Interaction with Accounts and Transactions
- Payroll service uses same Account and Transaction entities and DbContext; creates rows in Transactions with a synthetic “from” (e.g. system account) or a dedicated payroll source account, and to_account_id = employee account; or model as credit-only (to_account_number + amount). Current model: transaction has from_account_id and to_account_number; for salary, “from” could be a system/internal account and “to” the employee’s account number.

---

## 9. Scalability, Observability & Operations

### Scalability
- **Short term**: Single API instance + single PostgreSQL; connection pooling; index usage as above.
- **Growth**: Read replicas for GET /transactions, /account, /cards; write to primary. Later: shard by user_id or account_id if needed.
- **Caching**: Cache GET /api/accounts/me and /api/kyc/status per user (short TTL) to reduce DB load.

### Observability
- **Logs**: Structured logging (e.g. Serilog); request id; log level by endpoint (info for success, warning for 4xx, error for 5xx). No PII/card numbers in logs.
- **Metrics**: Request count and latency per route; transaction creation count and failure count; payroll run count and duration.
- **Traces**: Distributed trace id (e.g. OpenTelemetry) from API through service to DB.
- **Audit**: All critical actions above written to AuditLog table; retain per compliance policy.

### Deployment diagram (basic)
- **API server(s)**: ASP.NET Core; behind load balancer; env-specific config (DB connection, JWT secret).
- **Database**: PostgreSQL (single instance or primary+replicas).
- **Background worker**: Same process (IHostedService) or separate process for payroll runner.
- **Secrets**: Connection strings and JWT secret in key vault or env vars; never in source.

---

*Document version: 1.0 – Phase 3 System Design. Use with Phase 1 (Planning) and Phase 2 (Requirements) for implementation and backlog.*
