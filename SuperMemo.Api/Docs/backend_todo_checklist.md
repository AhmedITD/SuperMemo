## Backend Feature Checklist – Virtual Banking API

Use this checklist to verify that all **backend features** are implemented and tested.
Mark each item as done (`[x]`) or not done (`[ ]`).

### ✅ Overall Status: **~98% Complete**

**Completed Sections:**
- ✅ Users & Authentication (100%)
- ✅ KYC / KYB (100%)
- ✅ Accounts (100%)
- ✅ Admin Approval Flow (100%)
- ✅ Cards (100%)
- ✅ Transactions (100%)
- ✅ Payroll / Salary (100%)
- ✅ Security & Validation (100%)
- ⚠️ Logging, Errors & Testing (90% - Missing automated tests)

**Additional Features Implemented (Beyond Original Checklist):**
- ✅ Payment Gateway Integration (QiCard) - Phase 9
- ✅ Advanced Transaction Features (Risk Scoring, Retry Logic, Expiration)
- ✅ Account Types (Savings with interest, Regular)
- ✅ Daily Spending Limits for Savings Accounts
- ✅ Interest Calculation Background Service
- ✅ Dashboard & Analytics Endpoints
- ✅ Profile Management
- ✅ Phone Verification System (OTP)
- ✅ Password Reset Flow
- ✅ Refresh Token System
- ✅ Transaction Status History
- ✅ Fraud Detection Rules
- ✅ Merchant Accounts

---

## 1. Users & Authentication

- [x] **User registration**
  - [x] Endpoint `POST /auth/register` created.
  - [x] Accepts `FullName`, `Phone`, `Password`, `VerificationCode` (phone-based registration).
  - [x] Validates phone + password format (FluentValidation).
  - [x] Hashes password before saving (BCrypt, work factor 12).
- [x] **User login**
  - [x] Endpoint `POST /auth/login` created.
  - [x] Returns JWT `Token`, `RefreshToken`, `RefreshTokenExpiresAt` and user info.
  - [x] Handles invalid credentials safely (no sensitive error details).
- [x] **Current user endpoint**
  - [x] Endpoint `GET /auth/Me` returns user profile with `kyc_status`, `approval_status`, `role`.
  - [x] Requires valid JWT (auth middleware in place).
- [x] **Roles**
  - [x] Only roles `Customer` and `Admin` exist (UserRole enum).
  - [x] Role-based authorization added (`[Authorize(Policy = "Admin")]` on admin endpoints).

---

## 2. KYC / KYB (Identity Verification)

- [x] **KYC base**
  - [x] `Users.kyc_status` and `kyb_status` fields exist (KycStatus, KybStatus enums).
  - [x] KYC/KYB statuses: `Pending`, `Verified`, `Rejected`.
- [x] **IC (Identity Card) submission**
  - [x] Endpoint `POST /api/kyc/ic` implemented.
  - [x] Stores: `identity_card_number`, `full_name`, `mother_full_name`, `birth_date`, `birth_location`, `image_url`.
  - [x] Sets IC record status to `Pending`.
- [x] **Passport submission**
  - [x] Endpoint `POST /api/kyc/passport` implemented.
  - [x] Stores: `passport_number`, `full_name`, `short_name`, `nationality`, `birth_date`, `mother_full_name`, `expiry_date`, `image_url`.
  - [x] Sets Passport record status to `Pending`.
- [x] **Living Identity submission**
  - [x] Endpoint `POST /api/kyc/living-identity` implemented.
  - [x] Stores: `serial_number`, `full_family_name`, `living_location`, `form_number`, `image_url`.
  - [x] Sets LivingIdentity record status to `Pending`.
- [x] **KYC status endpoint**
  - [x] Endpoint `GET /api/kyc/status` returns current KYC type and status for logged-in user.
- [x] **KYC verification logic**
  - [x] Admin approval updates document status to `Verified` or `Rejected` (`PUT /api/admin/kyc/ic/{id}/verify`, etc.).
  - [x] `Users.kyc_status` is updated correctly based on KYC document decisions.

---

## 3. Accounts (One Account per User)

- [x] **Account model**
  - [x] `Accounts` table exists with fields: `id`, `user_id`, `balance`, `currency`, `status`, `account_type`, `account_number`, timestamps.
  - [x] One account per user is enforced (unique constraint on `user_id` via `IX_Accounts_UserId`).
- [x] **Account states**
  - [x] Status values: `PendingApproval`, `Active`, `Frozen`, `Closed` (AccountStatus enum).
  - [x] New accounts start as `PendingApproval`.
- [x] **Account endpoint**
  - [x] Endpoint `GET /api/accounts/me` returns current user's account summary.

---

## 4. Admin Approval Flow

- [x] **List users for admin**
  - [x] Endpoint `GET /api/admin/users` (filter by `approval_status` query parameter).
  - [x] Restricted to admin role (`[Authorize(Policy = "Admin")]`).
- [x] **Get user + KYC details**
  - [x] Endpoint `GET /api/admin/users/{id}` returns user, KYC data, and account info.
- [x] **Approve user**
  - [x] Endpoint `POST /api/admin/users/{id}/approval` implemented (with `action: "approve"` or `"reject"`).
  - [x] Sets `user.approval_status = 'Approved'`.
  - [x] Sets `user.kyc_status = 'Verified'` when appropriate.
  - [x] Sets `account.status = 'Active'`.
  - [x] All changes in one DB transaction.
- [x] **Reject user**
  - [x] Endpoint `POST /api/admin/users/{id}/approval` with `action: "reject"` implemented.
  - [x] Sets `user.approval_status = 'Rejected'` and `kyc_status = 'Rejected'`.
  - [x] Returns rejection reason to frontend (via request body).

---

## 5. Cards

- [x] **Card model**
  - [x] `Cards` table exists with fields:  
        `id`, `account_id`, `number`, `type`, `expiry_date`, `sc_hashed`,  
        `is_active`, `is_expired`, `is_employee_card`, `created_at`.
  - [x] `number` is unique (`IX_Cards_Number` unique index).
- [x] **List my cards**
  - [x] Endpoint `GET /api/accounts/me/cards` returns current user's cards.
- [x] **Issue card (admin)**
  - [x] Endpoint `POST /api/admin/cards` created (admin only).
  - [x] Accepts `account_id`, `type`, `is_employee_card`.
  - [x] Automatically generates card `number`, `sc_hashed` (BCrypt), `expiry_date`.
  - [x] Validates account is `Active`.
- [x] **Card status logic**
  - [x] `is_active` and `is_expired` correctly updated (via CardService logic).
  - [x] Transactions check card is active and not expired before use (TransactionService validation).

---

## 6. Transactions (Send Money)

- [x] **Transaction model**
  - [x] `Transactions` table exists with fields:  
        `id`, `from_account_id`, `to_account_number`, `amount`,  
        `status` (`Created` | `Pending` | `Sending` | `Completed` | `Failed` | `Expired`),  
        `purpose`, `idempotency_key`, `category`, `payment_id`, `created_at`.
- [x] **Create transaction endpoint**
  - [x] `POST /api/transactions/transfer` accepts `from_account_id`, `to_account_number`, `amount`, `purpose`.
  - [x] Uses `Idempotency-Key` header or body `idempotency_key`.
  - [x] Validates:
    - [x] Authenticated user.
    - [x] User `approval_status = 'Approved'`.
    - [x] KYC verified if required.
    - [x] Account status = `Active`.
    - [x] Amount > 0 and within allowed limits (daily spending limits for Savings accounts).
    - [x] Enough balance (if same-system debit).
  - [x] Creates transaction with status `Created`.
- [x] **Transaction lifecycle**
  - [x] Logic to move status `Created → Pending → Sending → Completed` when processing succeeds (via TransactionProcessingHostedService).
  - [x] On error, sets status to `Failed` and rolls back any balance changes.
- [x] **Idempotency**
  - [x] Unique constraint on (`from_account_id`, `idempotency_key`) via `IX_Transactions_FromAccountId_IdempotencyKey`.
  - [x] Second request with same key returns the same transaction instead of creating a new one.
- [x] **Transaction history**
  - [x] Endpoint `GET /api/transactions/account/{accountId}` returns list (with filters like status, pagination).
  - [x] Endpoint `GET /api/transactions/{id}` returns single transaction details.

---

## 7. Payroll / Salary (Optional Feature)

- [x] **PayrollJobs model**
  - [x] `PayrollJobs` table exists with fields:  
        `id`, `employee_user_id`, `employer_id`, `amount`, `currency`,  
        `schedule`, `next_run_at`, `status`, timestamps.
- [x] **Create payroll job (admin)**
  - [x] Endpoint `POST /api/admin/payroll` implemented.
  - [x] Validates employee user and account are valid and active.
- [x] **Payroll runner**
  - [x] Background job implemented (`PayrollRunnerHostedService`) to run payroll on schedule.
  - [x] Selects due `PayrollJobs` by `next_run_at` and `status='Active'`.
  - [x] Creates salary transactions for each job.
  - [x] Updates `next_run_at` to next scheduled date.
  - [x] Payroll is idempotent (no double salary for same period via idempotency keys).

---

## 8. Security & Validation

- [x] **JWT auth middleware** applied to all protected routes (`[Authorize]` attribute, JWT Bearer authentication).
- [x] **Input validation** for all endpoints (FluentValidation validators for all DTOs).
- [x] **Password hashing** using a strong algorithm (BCrypt with work factor 12).
- [x] **Card security**
  - [x] `sc_hashed` is stored hashed (BCrypt, no plain SC/CVV in DB).
  - [x] Card `number` is masked in logs and most responses (masking logic in CardService).
- [x] **KYC data protection**
  - [x] No raw KYC documents or very sensitive info appear in logs (audit logging with entity IDs only).
  - [x] Access to KYC endpoints and admin KYC views restricted properly (`[Authorize]` and `[Authorize(Policy = "Admin")]`).

---

## 9. Logging, Errors & Testing (Backend)

- [x] **Error format**
  - [x] Common JSON error response format defined (`ApiResponse<T>` with `success`, `data`, `message`, `code`, `errors`).
  - [x] Important error codes implemented (`USER_NOT_APPROVED`, `KYC_PENDING`, `ACCOUNT_INACTIVE`, `INSUFFICIENT_FUNDS`, `INVALID_VERIFICATION_CODE`, `DAILY_LIMIT_EXCEEDED`, etc. in ErrorCodes class).
- [x] **Logging**
  - [x] Logs for key actions: registration, login failures, KYC submissions, admin approvals, card issuance, transaction status changes, payroll runs (via ILogger and audit logging).
- [x] **Basic automated tests** ⚠️ *Test project structure created, tests need to be implemented*
  - [x] Test project structure created (`SuperMemo.Tests` with xUnit, Moq, FluentAssertions).
  - [x] Example unit tests created (TransactionService idempotency, insufficient funds).
  - [x] Example integration tests created (Auth endpoints).
  - [ ] **TODO**: Expand unit tests for all core business logic (transactions, idempotency, KYC checks).
  - [ ] **TODO**: Expand API tests for all main endpoints (happy path + important errors).
  - [ ] **TODO**: Add test database setup and seeding.
  - [ ] **TODO**: Configure CI/CD pipeline to run tests.

