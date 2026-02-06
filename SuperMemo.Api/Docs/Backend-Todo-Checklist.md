# Backend Feature Checklist – Virtual Banking API

Status of backend features against the original checklist.  
Implemented routes use prefix `/api` and `/auth` where applicable.

---

## 1. Users & Authentication

- [x] **User registration**
  - [x] Endpoint `POST /auth/register` created.
  - [x] Accepts `FirstName`, `LastName`, `Email`, `Phone` (optional), `Password`.
  - [x] Validates email uniqueness and password; phone optional.
  - [x] Password hashed before saving.
- [x] **User login**
  - [x] Endpoint `POST /auth/login` created.
  - [x] Returns JWT `Token`, `RefreshToken`, `RefreshTokenExpiresAt` and user info.
  - [x] Invalid credentials return generic error (no sensitive details).
- [x] **Current user endpoint**
  - [x] Endpoint `GET /auth/Me` returns user profile (includes `kyc_status`, `approval_status`, `role` via account/KYC).
  - [x] Requires valid JWT (auth middleware in place).
- [x] **Roles**
  - [x] Roles `Customer` and `Admin` only.
  - [x] Admin endpoints protected with `[Authorize(Policy = "Admin")]`.

---

## 2. KYC / KYB (Identity Verification)

- [x] **KYC base**
  - [x] `Users.kyc_status` and `kyb_status` exist.
  - [x] Statuses: `pending`, `verified`, `rejected` (and document-level statuses).
- [x] **IC (Identity Card) submission**
  - [x] Endpoint `POST /api/kyc/ic` implemented.
  - [x] Stores identity card fields; sets record status to `pending`.
- [x] **Passport submission**
  - [x] Endpoint `POST /api/kyc/passport` implemented.
  - [x] Stores passport fields; sets record status to `pending`.
- [x] **Living Identity submission**
  - [x] Endpoint `POST /api/kyc/living-identity` implemented.
  - [x] Stores living identity fields; sets record status to `pending`.
- [x] **KYC status endpoint**
  - [x] Endpoint `GET /api/kyc/status` returns current KYC type and status for logged-in user.
- [x] **KYC verification logic**
  - [x] Admin endpoints update document status to `verified` or `rejected`.
  - [x] `Users.kyc_status` updated from document verification decisions.

---

## 3. Accounts (One Account per User)

- [x] **Account model**
  - [x] `Accounts` table with `id`, `user_id`, `account_number`, `balance`, `currency`, `status`, timestamps.
  - [x] One account per user (unique `user_id`).
- [x] **Account states**
  - [x] Status values: `pending_approval`, `active`, `frozen`, `closed`.
  - [x] Account created on admin approval and starts as `active` (no separate pending_approval account state; user has `ApprovalStatus`).
- [x] **Account endpoint**
  - [x] Endpoint `GET /api/accounts/me` returns current user’s account summary.

---

## 4. Admin Approval Flow

- [x] **List users for admin**
  - [x] Endpoint `GET /api/admin/users` with optional `approvalStatus` filter and pagination.
  - [x] Restricted to admin role.
- [x] **Get user + KYC details**
  - [x] Endpoint `GET /api/admin/users/{id}` returns user, KYC status, and account info.
- [x] **Approve user**
  - [x] Endpoint `POST /api/admin/users/{userId}/approval` with body `{ "approvalStatus": "Approved" }`.
  - [x] Sets `user.approval_status = Approved`, creates/activates account (`account.status = active`) in one flow.
- [x] **Reject user**
  - [x] Same endpoint with `approvalStatus: Rejected`; sets `user.approval_status = Rejected`, closes account if any.
  - [ ] Optional: dedicated rejection reason field and returned to frontend (not implemented).

---

## 5. Cards

- [x] **Card model**
  - [x] `Cards` table with `id`, `account_id`, `number`, `type`, `expiry_date`, `sc_hashed`, `is_active`, `is_expired`, `is_employee_card`, timestamps.
  - [x] Card number unique.
- [x] **List my cards**
  - [x] Endpoint `GET /api/accounts/me/cards` returns current user’s cards.
- [x] **Issue card (admin)**
  - [x] Endpoint `POST /api/admin/cards` (admin only); accepts `account_id`, `type`, and card details.
  - [x] Admin provides `number`, `securityCode` (stored as `sc_hashed`), `expiryDate`; validates account is active.
  - [ ] Checklist “automatically generates number, sc_hashed, expiry_date”: not implemented; admin supplies them.
- [x] **Card status logic**
  - [x] `is_active` and `is_expired` on model; expiry can be enforced by jobs or on use.
  - [x] Transactions validate that the source account has at least one active, non-expired card (by `IsActive`, `!IsExpired`, and `ExpiryDate >= today`) before allowing a transfer; error code `NO_ACTIVE_CARD` when not met.

---

## 6. Transactions (Send Money)

- [x] **Transaction model**
  - [x] `Transactions` table with `id`, `from_account_id`, `to_account_number`, `amount`, `status`, `idempotency_key`, timestamps, etc.
  - [x] Status flow: `Created` → `Sending` → `Completed` (or `Failed`).
- [x] **Create transaction endpoint**
  - [x] `POST /api/transactions/transfer` accepts transfer payload; uses `Idempotency-Key` header or body `idempotency_key`.
  - [x] Validates: authenticated user, account active, amount > 0, sufficient balance.
  - [x] Creates transaction and processes (status progression).
- [x] **Transaction lifecycle**
  - [x] Status moves to `Sending` then `Completed` on success; on error `Failed` and balance rollback.
- [x] **Idempotency**
  - [x] Unique constraint on (`from_account_id`, `idempotency_key`).
  - [x] Duplicate key returns existing transaction (no duplicate creation).
- [x] **Transaction history**
  - [x] `GET /api/transactions/account/{accountId}` returns list for account (pagination).
  - [x] `GET /api/transactions/{id}` returns single transaction.

---

## 7. Payroll / Salary (Optional Feature)

- [x] **PayrollJobs model**
  - [x] Table with `id`, `employee_user_id`, `employer_id`, `amount`, `currency`, `schedule`, `next_run_at`, `status`, timestamps.
- [x] **Create payroll job (admin)**
  - [x] Endpoint `POST /api/admin/payroll` implemented; validates employee and account.
- [x] **Payroll runner**
  - [x] Hosted service runs payroll on schedule; selects due jobs by `next_run_at` and status.
  - [x] Creates salary transactions; updates `next_run_at`; idempotent per period (e.g. `payroll_{jobId}_{yyyyMM}`).

---

## 8. Security & Validation

- [x] **JWT auth middleware** on protected routes.
- [x] **Input validation** (FluentValidation / model validation) on endpoints.
- [x] **Password hashing** (strong algorithm).
- [x] **Card security**
  - [x] `sc_hashed` stored hashed; card number masked in responses where appropriate.
- [x] **KYC data protection**
  - [x] KYC and admin KYC endpoints restricted; sensitive data not logged.

---

## 9. Logging, Errors & Testing (Backend)

- [x] **Error format**
  - [x] Common JSON error response with `code`, `message`.
  - [x] Error codes: e.g. `EMAIL_ALREADY_EXISTS`, `INSUFFICIENT_FUNDS`, `VALIDATION_FAILED`, `IDEMPOTENT_REPLAY`, etc.
- [x] **Logging**
  - [x] Audit and logs for registration, login, KYC, admin approvals, card issuance, transaction changes, payroll runs.
- [ ] **Basic automated tests**
  - [ ] Unit tests for core logic (transactions, idempotency, KYC).
  - [ ] API tests for main endpoints (happy path and key errors).

---

## Summary

- **Done:** All checklist areas are implemented except optional “rejection reason”, card auto-generation, card checks in transactions, and automated tests.
- **API base:** Auth under `/auth`, rest under `/api` (e.g. `/api/kyc`, `/api/accounts`, `/api/admin`, `/api/transactions`, `/api/admin/payroll`).
