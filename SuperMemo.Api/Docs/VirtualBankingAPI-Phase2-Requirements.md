# Virtual Banking API – Phase 2: Requirements & Analysis

---

## 1. Functional Requirements by Module

### Users & Authentication
- **FR-AUTH-1** A customer can register with email, password, first name, last name, and optional phone; role is set to customer.
- **FR-AUTH-2** A user can log in with email and password and receive a JWT and refresh token.
- **FR-AUTH-3** Authenticated user can call `GET /auth/Me` to get current user profile (id, name, email, phone, role, kyc_status, kyb_status, approval_status).
- **FR-AUTH-4** Passwords are hashed (e.g. bcrypt) and never stored in plain text.
- **FR-AUTH-5** Admin users can be created (e.g. seeded or separate admin registration); customers cannot self-assign admin role.

### KYC / KYB
- **FR-KYC-1** A customer can submit exactly one document type: Identity Card (IC), Passport, or Living Identity, via `POST /api/kyc/ic`, `POST /api/kyc/passport`, or `POST /api/kyc/living-identity`.
- **FR-KYC-2** Each document type has required fields; submission is validated; document status is stored as `pending | verified | rejected`.
- **FR-KYC-3** Customer can call `GET /api/kyc/status` to see current KYC (and optionally KYB) status and which document type was submitted.
- **FR-KYC-4** Admin can verify or reject each KYC document via admin endpoints; user's `kyc_status` is updated accordingly.
- **FR-KYC-5** Re-submission: after rejection, customer may submit a new document (same or different type) per product policy; implementation may allow one active submission per type.

### Admin Approval
- **FR-ADMIN-1** Admin can list users (customers) with optional filter by `approval_status` (`pending_approval | approved | rejected`), with paging.
- **FR-ADMIN-2** Admin can approve or reject a user via `POST /api/admin/users/{userId}/approval` with body `{ "approvalStatus": "Approved" | "Rejected" }`; on approval, an account is created for the user if it does not exist and set to `active`.
- **FR-ADMIN-3** Admin can set account status to `active | frozen | closed` via `PUT /api/admin/accounts/{accountId}/status`.
- **FR-ADMIN-4** Only users with `approval_status == approved` and account `status == active` can perform transfers and use cards.

### Accounts
- **FR-ACC-1** Each approved user has exactly one account; account has `balance`, `currency`, `status` (`pending_approval | active | frozen | closed`), and a unique `account_number`.
- **FR-ACC-2** Customer can get their own account via `GET /api/accounts/me`; response includes account_number, balance, currency, status.
- **FR-ACC-3** Balance changes only via transactions (debit/credit); no direct balance set by API.
- **FR-ACC-4** Frozen or closed accounts cannot send or receive transfers.

### Cards
- **FR-CARD-1** Only admin can create (issue) a card via `POST /api/admin/cards` with account_id, number, type (virtual | MasterCard | VisaCard), expiry_date, security_code (plain, then hashed), and optional `is_employee_card`.
- **FR-CARD-2** Card number is unique; security code is stored only as hash (e.g. bcrypt); never returned in API.
- **FR-CARD-3** Cards have `is_active` and `is_expired`; admin or system can revoke (set `is_active = false`); `is_expired` can be derived from `expiry_date`.
- **FR-CARD-4** Customer can list their cards via `GET /api/accounts/me/cards`; response shows masked number (e.g. ****1234), type, expiry, is_active, is_expired, is_employee_card.
- **FR-CARD-5** Admin can list cards by account and revoke a card.

### Transactions
- **FR-TXN-1** Customer creates a transfer via `POST /api/transactions/transfer` with `from_account_id`, `to_account_number`, `amount`, `purpose` (optional), and `idempotency_key`.
- **FR-TXN-2** Transaction status follows lifecycle: `Created` → `sending` → `completed` or `failed`.
- **FR-TXN-3** Idempotency: if the same `idempotency_key` is sent again for the same `from_account_id`, the API returns the same result as the first request (same transaction id and status) without creating a duplicate transfer.
- **FR-TXN-4** Validations: amount > 0; from_account belongs to current user and is active; to_account exists and is active; sufficient balance.
- **FR-TXN-5** Customer can list transactions for their account with optional filters (status, date range) and paging via `GET /api/transactions/account/{accountId}`.
- **FR-TXN-6** Customer can get a single transaction by id via `GET /api/transactions/{id}` (only if the transaction belongs to the user's account).

### Transaction History
- **FR-HIST-1** List endpoint supports filter by `status` (Created, sending, completed, failed) and paging (pageNumber, pageSize).
- **FR-HIST-2** Results ordered by created_at descending.

### Payroll
- **FR-PAY-1** Admin can create a PayrollJob via `POST /api/admin/payroll` with employee_user_id, employer_id (optional), amount, currency, schedule, next_run_at.
- **FR-PAY-2** Admin can list PayrollJobs with optional filters and paging via `GET /api/admin/payroll`.
- **FR-PAY-3** Admin can get, update (amount, schedule, status, next_run_at), and delete a PayrollJob.
- **FR-PAY-4** A background/scheduled process runs payroll: for each active job with `next_run_at` due, it creates a credit transaction to the employee's account (idempotent per job + period); then updates `next_run_at` (e.g. next month).

---

## 2. Non-Functional Requirements (NFRs)

### Security
- **NFR-S1** All sensitive data in transit use TLS (HTTPS in production).
- **NFR-S2** Passwords hashed with strong algorithm (e.g. bcrypt, work factor ≥ 12); tokens (JWT) signed and validated.
- **NFR-S3** Card security code never stored in plain text; card number masked in logs and in list responses.
- **NFR-S4** KYC document content and PII accessed only by authorized roles; audit log for access to KYC and approval actions.
- **NFR-S5** Admin endpoints protected by role-based authorization (Admin only).

### Performance
- **NFR-P1** Target response time for read endpoints (account, list transactions, list cards): p95 &lt; 500 ms under normal load.
- **NFR-P2** Transfer creation (POST transfer): p95 &lt; 2 s including balance update and transaction record.
- **NFR-P3** Expected transaction volume for MVP: on the order of hundreds of transactions per day; design should allow scaling.

### Availability & Reliability
- **NFR-A1** Database and API should be deployable in a way that allows recovery from failure (backups, health checks).
- **NFR-A2** Transfer operation is atomic: balance update and transaction record committed together (single transaction boundary).

### Observability
- **NFR-O1** Logging: request/response for critical operations (login, approval, transfer, card issue); no sensitive data in logs.
- **NFR-O2** Audit log: record who approved/rejected which user; who issued/revoked which card; transaction creation (from/to/amount/status).
- **NFR-O3** Health endpoint for liveness/readiness (e.g. DB connectivity).

### Compliance & Data Retention
- **NFR-C1** KYC and transaction data retained according to regulatory requirements; retention period to be defined.
- **NFR-C2** Ability to provide audit trail for KYC decisions and financial transactions.

---

## 3. Detailed Use Cases & Flows

### UC1: Customer registration + login
- **Happy path:** Customer submits register (email, password, name, phone). System creates user with role customer, kyc_status and approval_status pending. Customer then logs in with email/password; receives JWT and refresh token; can call /auth/Me.
- **Alternate:** Email already exists → 400 with error code (e.g. EMAIL_ALREADY_EXISTS). Invalid login → 401.

### UC2: Customer submits KYC (IC / Passport / LivingIdentity)
- **Happy path:** Customer chooses one document type, fills required fields, calls POST /api/kyc/ic (or passport / living-identity). System stores document with status pending and sets user kyc_status to pending. Admin later verifies or rejects.
- **Alternate:** Missing required field → 400 validation. User already has a verified document of same type → business rule may allow or deny re-submission (e.g. 400 KYC_ALREADY_SUBMITTED).

### UC3: Admin reviews KYC and approves/rejects user + account
- **Happy path:** Admin lists users with approval_status = pending_approval; opens user; reviews KYC; calls POST /api/admin/users/{id}/approval with approvalStatus: Approved. System sets user approval_status = approved, creates account if not exists, sets account status = active.
- **Alternate:** Reject: approvalStatus: Rejected; user approval_status = rejected; if account exists, may set to closed. Admin sets account to frozen via PUT account status.

### UC4: Admin issues card (optional is_employee_card)
- **Happy path:** Admin calls POST /api/admin/cards with accountId, number, type (virtual/MasterCard/VisaCard), expiryDate, securityCode, is_employee_card. System hashes security code, stores card with is_active true, is_expired from expiry_date.
- **Alternate:** Duplicate card number → 400 CARD_NUMBER_EXISTS. Account not found or not active → 400.

### UC5: Customer sends money
- **Happy path:** Customer calls POST /api/transactions/transfer with from_account_id (own account), to_account_number, amount, purpose, idempotency_key. System validates account ownership and active status, sufficient balance, to_account exists and active; creates transaction (Created → sending); debits from_account, credits to_account; sets transaction completed; returns transaction response.
- **Alternate:** Insufficient balance → 400 INSUFFICIENT_FUNDS. Account not active → 400 ACCOUNT_INACTIVE. To account not found → 400 DESTINATION_ACCOUNT_NOT_FOUND.

### UC6: Idempotency for send money
- **Happy path:** Same request (same from_account_id, idempotency_key) sent again within retention window. System finds existing transaction with that key; returns same transaction id and status (e.g. completed) without modifying balance again.
- **Alternate:** First request failed (e.g. insufficient funds); second request with same key returns same error (no new transaction). Idempotency key empty or invalid format → 400.

### UC7: Salary payroll run
- **Happy path:** Scheduler runs at scheduled time; for each PayrollJob with status active and next_run_at ≤ now, finds employee's account; creates a credit transaction (salary) to that account (idempotent per job+period); updates next_run_at (e.g. add 1 month). Job status remains active.
- **Alternate:** Employee has no account or account closed → skip or mark job failed; alert. Duplicate run for same period → idempotent credit, no double credit.

---

## 4. Validation Rules & Business Rules

### Users
- Email: required, valid format, unique.
- Password: required, min length (e.g. 8), complexity per policy (e.g. uppercase, number, symbol).
- Phone: optional; if present, valid format (e.g. E.164 or national format).

### KYC
- IC: identity_card_number, full_name, mother_full_name, birth_date, birth_location required. Status transitions: pending → verified | rejected.
- Passport: passport_number, full_name, nationality, birth_date, mother_full_name, expiry_date required. Same status transitions.
- Living Identity: serial_number, full_family_name, living_location, form_number required. Same status transitions.
- Resubmission: after rejection, user may submit new document; product may limit one pending per type.

### Accounts
- Only approved users can have active accounts. Frozen/closed accounts cannot send or receive. One account per user.

### Cards
- is_expired = (expiry_date ≤ today). Revoking sets is_active = false. Transactions reference account; card type/metadata do not block transfer (MVP).

### Transactions
- Amount &gt; 0. Single currency for MVP (e.g. same currency for from and to). Status: Created → sending → completed | failed. No backward status transition.

### Idempotency
- Same idempotency_key + from_account_id returns same response (same transaction or same error). Key length/format validated (e.g. max 64, non-empty). Retention: e.g. 24h or 7 days for key lookup.

### PayrollJobs
- Only for users with an account. If employer balance is modeled, rule for insufficient employer balance (e.g. fail job or alert). Otherwise salary is “injected” from system.

---

## 5. Error Handling & API Contracts

### Standard error response format
```json
{
  "success": false,
  "message": "Human-readable message",
  "code": "ERROR_CODE",
  "errors": { "fieldName": ["validation error 1"] }
}
```

### Error codes (examples)
| Code | When |
|------|------|
| EMAIL_ALREADY_EXISTS | Register with existing email |
| INVALID_CREDENTIALS | Login failed |
| USER_NOT_APPROVED | Action requires approved user |
| KYC_PENDING | Action requires verified KYC |
| KYC_REJECTED | KYC was rejected |
| ACCOUNT_INACTIVE | Account frozen/closed |
| DESTINATION_ACCOUNT_NOT_FOUND | to_account_number not found |
| INSUFFICIENT_FUNDS | Balance &lt; amount |
| CARD_NUMBER_EXISTS | Duplicate card number |
| CARD_EXPIRED | Card expired (if used in context) |
| IDEMPOTENT_REPLAY | Same idempotency key returned previous result (can use 200 with same body) |
| VALIDATION_FAILED | FluentValidation errors |
| RESOURCE_NOT_FOUND | Entity not found (e.g. transaction id) |

### Example request/response (Auth)
- **POST /auth/register** – Success: 201, body `{ "success": true, "data": { "id", "token", "refreshToken", ... } }`. Error: 400, `{ "success": false, "code": "EMAIL_ALREADY_EXISTS", "message": "Email already exists." }`.
- **POST /auth/login** – Success: 200 with token. Error: 400, `{ "code": "INVALID_CREDENTIALS", "message": "Invalid credentials." }`.

### Example (Transactions)
- **POST /api/transactions/transfer** – Success: 200, `{ "success": true, "data": { "id", "fromAccountId", "toAccountNumber", "amount", "status": "Completed", ... } }`. Error: 400, `{ "code": "INSUFFICIENT_FUNDS", "message": "Insufficient balance." }`. Idempotent replay: 200 with same data as first request.

### Example (KYC)
- **GET /api/kyc/status** – Success: 200, `{ "success": true, "data": { "kycStatus": "Pending", "kybStatus": "Pending", "documentType": "Ic", "documentStatus": "Pending" } }`.

---

## 6. Assumptions, Constraints & Open Questions (Requirements View)

### Assumptions
- One currency for MVP; all accounts and transactions in that currency.
- Salaries are fixed amounts per PayrollJob; monthly schedule.
- Only internal transfers (between accounts in this system); no outbound bank transfers in MVP.
- Admin users are trusted; no separate “notes” or “re-open” in MVP unless specified.
- Idempotency key is client-generated (e.g. UUID); TTL for key storage to be defined (e.g. 7 days).

### Constraints
- One account per user. One KYC document type per user (or one active submission).
- Card number format and generation (e.g. BIN + random) to be defined; security code 3–4 digits.

### Open questions
- Max transaction amount and daily/monthly limits?
- KYC review SLA (e.g. 24h)?
- Salary run failure policy (retry, alert, pause job)?
- Exact idempotency key TTL and scope (per user vs per account)?
- Should GET /account be exactly `/account` or `/accounts/me`? (Current: `/accounts/me`.)

---

## 7. Acceptance Criteria Summary (Per Major Feature)

### Users / Auth
- Given valid registration data, when customer registers, then user is created with role customer and can log in.
- Given valid credentials, when customer logs in, then receives JWT and can call /auth/Me.
- Given duplicate email, when customer registers, then receives 400 with EMAIL_ALREADY_EXISTS.

### KYC
- Given logged-in customer, when submits IC (or Passport / LivingIdentity) with valid data, then document is stored with status pending and kyc_status is pending.
- Given customer with pending KYC, when calls GET /api/kyc/status, then receives current kycStatus and document status.
- Given admin, when verifies document, then user kyc_status updates to verified (or rejected).

### Admin Approval
- Given pending user, when admin approves, then user approval_status = approved and account is created/activated.
- Given approved user, when admin sets account to frozen, then user cannot perform transfers.

### Cards
- Given admin and valid payload, when admin issues card, then card is stored with hashed security code and customer can see it in GET /api/accounts/me/cards (masked).
- Given card, when admin revokes, then is_active = false.

### Transactions
- Given active account and sufficient balance, when customer sends transfer with idempotency_key, then transaction is created and completed and balance updated.
- Given same idempotency_key again, when customer sends again, then same transaction response returned and balance unchanged.
- Given insufficient balance, when customer sends transfer, then 400 INSUFFICIENT_FUNDS.

### Payroll
- Given active PayrollJob and employee with account, when payroll runs, then salary credit transaction is created and next_run_at updated.
- Given admin, when lists GET /api/admin/payroll, then receives paginated list of payroll jobs.
