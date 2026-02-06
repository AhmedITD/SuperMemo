# Virtual Banking API – Phase 5: Testing & QA Strategy

**Scope:** Backend API (Virtual Banking – Customer Wallet with Linked Cards).  
**Stack:** .NET 8, ASP.NET Core, EF Core, PostgreSQL.

---

## 1. Overall Test Strategy & Scope

### Test pyramid and types

| Level | Focus | Tools / approach |
|-------|--------|-------------------|
| **Unit** | Business logic in isolation (services, validators, domain rules). Mock DbContext, external deps. | xUnit/NUnit, Moq/FakeItEasy, FluentAssertions |
| **Integration** | API + DB: real HTTP calls to in-memory or test DB; full request/response and persistence. | WebApplicationFactory, TestContainers or local PostgreSQL |
| **API / Contract** | Request/response shape, status codes, error codes; optional OpenAPI/Swagger contract checks. | Integration tests + Assert on JSON/status |
| **E2E** | Multi-step flows across Auth → KYC → Admin → Account → Cards → Transactions (and Payroll). | Same as integration; orchestrated scenarios |
| **Performance / Load** | Throughput and latency under load (transactions, list endpoints). | k6, NBomber, or JMeter |
| **Security** | AuthZ, injection, token handling, no PII/card data in responses or logs. | Manual + automated (OWASP ZAP, auth tests in integration) |
| **Regression** | Fixed suite (unit + integration + critical E2E) run on every commit/PR and pre-deploy. | CI pipeline (e.g. GitHub Actions, Azure DevOps) |

### What each level focuses on

- **Unit:** Service methods with mocked `ISuperMemoDbContext`, `IPasswordService`, `ICurrentUser`, `IAuditEventLogger`; validators with valid/invalid inputs; error code mapping.
- **Integration:** Real app (or minimal host), real or test DB; HTTP client; assert status, body, and DB state (e.g. row inserted, balance updated).
- **E2E:** Full user journey (register → login → KYC → admin approval → get account → transfer) and failure paths (insufficient funds, not approved).
- **Performance:** Many concurrent POST /transactions, GET /transactions, GET /admin/users; measure p95/p99 latency, error rate, TPS.
- **Security:** No admin access with customer token; no access to other users’ accounts/transactions/KYC; JWT expiry and refresh; no card number/SC in response or logs.

### In scope (this project)

- All API endpoints listed in context (Auth, KYC, Account, Cards, Transactions, Admin, Payroll).
- Business rules: one account per user, approval/KYC gates, transaction lifecycle, idempotency, payroll idempotency.
- Error responses and codes (e.g. INSUFFICIENT_FUNDS, ACCOUNT_INACTIVE, VALIDATION_FAILED).
- Audit events for critical actions.
- Security: auth, roles, sensitive data handling.

### Out of scope (for now)

- Frontend or mobile clients (only backend API).
- Real payment network or card scheme integration (card type is metadata only).
- Advanced fraud detection or rate limiting implementation details (test only what exists).
- Full penetration testing by external party (covered by security test cases and tooling only).

---

## 2. Unit Test Plan (By Module)

### Users & Auth

- **Register:** Given valid request → user created with role Customer, email normalized, password hashed (verify hash differs from plain); duplicate email → ErrorResponse with EMAIL_ALREADY_EXISTS.
- **Login:** Valid credentials → returns success and token; wrong password → INVALID_CREDENTIALS; missing user → INVALID_CREDENTIALS.
- **JWT:** Token contains expected claims (sub, role, name, email); expired token rejected.
- **Refresh:** Valid refresh token → new JWT and refresh token returned; invalid/expired refresh → INVALID_REFRESH_TOKEN.
- **Role:** CurrentUser.Id and Role read from claims; admin policy requires role Admin.

### KYC (IC, Passport, Living Identity)

- **Submit IC:** Valid request → IcDocument created with status Pending, user.KycStatus set to Pending; required field missing → validation error.
- **Submit Passport:** Same pattern; expiry_date in past allowed for submission (reject at business rule if needed).
- **Submit Living Identity:** Same pattern; all required fields validated.
- **Get status:** User with no document → documentType null, documentStatus null; user with IC pending → documentType Ic, documentStatus Pending.
- **Admin verify:** Document status set to Verified/Rejected; user.KycStatus updated accordingly (Verified → KycStatus.Verified, etc.).

### Admin approval

- **List users:** Filter by approval_status returns only matching users; paging works (page 2 different from page 1).
- **Approve:** User has no account → account created with status Active and unique AccountNumber; user has account → account status set Active; user.ApprovalStatus = Approved.
- **Reject:** User approval_status = Rejected; if account exists, status set to Closed.
- **Invalid transition:** Approve already approved user → idempotent (no error); reject already rejected → idempotent.
- **Set account status:** Account status updated to Frozen/Closed/Active; only that account affected.

### Accounts

- **One per user:** Creating a second account for same user violates unique index (or service prevents it); approve flow creates at most one account per user.
- **Get my account:** User with approved account → returns account with balance, currency, status; user without account → RESOURCE_NOT_FOUND.
- **State:** Account in Frozen/Closed → cannot be used for transfer (service returns ACCOUNT_INACTIVE).

### Cards

- **Issue:** Valid request → card created, ScHashed not equal to plain security code, Number unique; duplicate Number → CARD_NUMBER_EXISTS; account not found → error.
- **Is_expired:** ExpiryDate in past → IsExpired true; in future → false.
- **Revoke:** Card IsActive set to false; second revoke idempotent.
- **List:** Only cards for given account; response has masked number (e.g. ****1234), no ScHashed.

### Transactions

- **Amount:** Zero or negative amount → validation or business error (amount must be positive).
- **Balance:** fromAccount.Balance < amount → INSUFFICIENT_FUNDS; balance unchanged after failed attempt.
- **Account status:** fromAccount or toAccount not Active → ACCOUNT_INACTIVE / DESTINATION_ACCOUNT_NOT_FOUND or not active.
- **Ownership:** fromAccount.UserId != current user → RESOURCE_NOT_FOUND or forbidden.
- **Status lifecycle:** New transaction created with status Sending then Completed after balance update; no backward transition (Completed → Sending).
- **Idempotency (unit):** Same (from_account_id, idempotency_key) second call returns same transaction response without creating a second row; balance debited/credited once.
- **Idempotency key empty:** Treated as no idempotency (each request creates new transaction if validation passes); or validation requires key (then 400).

**Given/When/Then examples (transaction creation):**

- **Given** user has active account with balance 100, **When** CreateTransfer(amount: 50, to_account_number valid, idempotency_key: "key1"), **Then** transaction created with status Completed, balance 50, and response 200 with transaction id.
- **Given** same as above, **When** CreateTransfer same request again with same idempotency_key "key1", **Then** 200 with same transaction id, balance still 50 (no second debit).
- **Given** balance 100, **When** CreateTransfer(amount: 150, ...), **Then** 400 INSUFFICIENT_FUNDS, balance unchanged 100, no transaction row or transaction with status Failed.

### PayrollJobs

- **Due selection:** Jobs with status Active and next_run_at <= now are selected; Paused/Canceled excluded; next_run_at null excluded (or included by rule).
- **Salary credit:** For each due job, CreatePayrollCreditAsync called with idempotency key payroll_{jobId}_{yyyyMM}; on success next_run_at advanced (e.g. AddMonths(1)).
- **Idempotency:** Same job+period run twice → second run does not create duplicate credit (same idempotency key).
- **Active/Paused/Canceled:** Only Active jobs run; updating job to Paused excludes it from next run.

---

## 3. Integration & API Test Plan

### Setup

- Use `WebApplicationFactory<Program>` (or equivalent) with test DB (TestContainers PostgreSQL or local test DB); run migrations; seed minimal data (one admin, one customer, one account) or create in test.
- Each test or suite: arrange (create user, login, get token), act (HTTP request with token), assert (status, JSON body, DB state).

### Auth

- **Register + Login + /me:** POST register → 201; POST login with same credentials → 200 with token; GET /auth/Me with Bearer token → 200 with user data (id, email, role, kyc_status, approval_status).
- **Invalid login:** POST login wrong password → 400, code INVALID_CREDENTIALS.
- **Unauthorized /me:** GET /auth/Me without token → 401.

### KYC

- **Submit IC + status:** POST /api/kyc/ic with valid body (auth as customer) → 200/201 with document id; GET /api/kyc/status → 200, documentType "Ic", documentStatus Pending.
- **Submit Passport / Living Identity:** Same pattern for respective endpoints and status.
- **Admin verify:** PUT /api/admin/kyc/ic/{documentId}/verify with body { status: "Verified" } (auth as admin) → 200; GET /api/kyc/status (as that user) → documentStatus Verified.

### Admin

- **List pending users:** GET /api/admin/users?approvalStatus=PendingApproval (admin) → 200, list of users; customer token → 403.
- **Approve user:** POST /api/admin/users/{userId}/approval { approvalStatus: "Approved" } → 200; GET /api/accounts/me (as that user) → 200, account status Active.
- **Reject user:** approvalStatus Rejected → user approval_status Rejected; account if exists set Closed.

### Cards

- **Issue + list:** POST /api/admin/cards with accountId, number, type, expiry, securityCode, is_employee_card (admin) → 200, card in response with masked number; GET /api/accounts/me/cards (as account owner) → 200, list contains that card with masked number.
- **Revoke:** PUT /api/admin/cards/{cardId}/revoke → 200; GET cards again → card is_active false.

### Transactions

- **Create transfer:** POST /api/transactions/transfer with from_account_id (own), to_account_number, amount, purpose, Idempotency-Key header (or body) → 200, transaction in response with status Completed; GET /api/transactions/account/{accountId} → list includes that transaction; DB: from account balance decreased, to account balance increased.
- **Idempotent replay:** Same request (same Idempotency-Key) again → 200, same transaction id and amount; balance unchanged (single debit/credit).
- **Insufficient balance:** amount > balance → 400, code INSUFFICIENT_FUNDS; balance unchanged.

### Payroll

- **List + create job:** GET /api/admin/payroll (admin) → 200 paginated; POST /api/admin/payroll with employee_user_id, amount, currency, next_run_at → 200; GET by id → 200.
- **Run payroll (trigger):** Either call a test endpoint that runs PayrollRunnerService.RunDueJobsAsync or advance time and run hosted service once; then GET employee transactions → salary transaction present; PayrollJob next_run_at advanced.

### DB expectations (examples)

- After approve: `Accounts` has one row for that user with status Active.
- After transfer: `Transactions` has one row status Completed; `Accounts` balances updated.
- After idempotent replay: still one transaction row for that key; balances unchanged.
- After payroll run: new row in `Transactions` for salary; `PayrollJobs.next_run_at` updated.

---

## 4. End-to-End Scenario Tests

### E2E-1: Register → Login → KYC → Admin approve → Account active

1. POST /auth/register (customer).
2. POST /auth/login → token.
3. POST /api/kyc/ic (with token).
4. As admin: GET /api/admin/users?approvalStatus=PendingApproval → see user; POST /api/admin/users/{id}/approval { approvalStatus: "Approved" }.
5. As customer: GET /api/accounts/me → 200, account status Active, balance 0.
6. **Final state:** User approved, one account Active, KYC document Pending or Verified.

### E2E-2: After approval, admin issues card, customer sees it

1. Use approved customer and get token; GET /api/accounts/me → accountId.
2. As admin: POST /api/admin/cards { accountId, number, type, expiryDate, securityCode, is_employee_card }.
3. As customer: GET /api/accounts/me/cards → 200, one card, masked number, type/expiry correct.
4. **Final state:** Card exists for account; customer cannot see full number or SC.

### E2E-3: Send money – success path

1. Two approved users A and B, each with account; note A’s account id and B’s account number.
2. As A: POST /api/transactions/transfer { from_account_id (A’s), to_account_number (B’s), amount, purpose, Idempotency-Key: "e2e3-key" } → 200, status Completed.
3. As A: GET /api/transactions/account/{A’s accountId} → list includes transaction, status Completed.
4. As A: GET /api/accounts/me → balance decreased by amount; as B: balance increased.
5. **Final state:** One transaction Completed; balances consistent.

### E2E-4: Insufficient balance – failure

1. User A with balance 50.
2. POST transfer amount 100 → 400 INSUFFICIENT_FUNDS.
3. GET /api/accounts/me → balance still 50; GET transactions → no new completed transaction (or one Failed if implemented).
4. **Final state:** Balance unchanged; no double debit.

### E2E-5: Pending KYC / not approved – blocked from transfer

1. User registered but not approved (or KYC not submitted).
2. If account not yet created: GET /api/accounts/me → 404 or no account.
3. If account exists but Frozen/Closed or user not approved: POST transfer → 400 (ACCOUNT_INACTIVE or RESOURCE_NOT_FOUND / USER_NOT_APPROVED per implementation).
4. **Final state:** No transfer possible until approved and account active.

### E2E-6: Payroll run credits employee

1. Admin creates PayrollJob for employee user (with account), amount, next_run_at in past or now.
2. Set Payroll:SourceAccountNumber to a system account that has balance (or mock runner to use test source).
3. Trigger payroll run (hosted service or test endpoint).
4. As employee: GET /api/transactions/account/{id} → new salary transaction; GET /api/accounts/me → balance increased by job amount.
5. **Final state:** PayrollJob next_run_at advanced; one salary transaction; idempotent (run again same period → no duplicate credit).

---

## 5. Idempotency & Concurrency Tests

### Idempotency

- **Rapid duplicate:** Send same POST /transactions/transfer (same from_account_id, to_account_number, amount, Idempotency-Key) 5 times in quick succession. **Assert:** 200 every time; same transaction id in response; DB has exactly one transaction row for that key; from-account balance debited once.
- **After completed:** First request completes; second request with same key 1 minute later → 200, same transaction id and payload; balance unchanged.
- **Header vs body:** Send Idempotency-Key in header only (body without key) → success; send same key in body on next request → same transaction returned.

### Concurrency

- **Overspend:** Account balance 100. Fire two requests in parallel: transfer 60 and transfer 60 (different idempotency keys). **Assert:** One succeeds (200), one fails (400 INSUFFICIENT_FUNDS); final balance 40; exactly one completed transaction for the successful key. Use DB transaction isolation / locking so that the second request sees updated balance or fails correctly.
- **Same key concurrent:** Two requests with same Idempotency-Key at same time. **Assert:** Only one transaction row created (unique index or application logic); both responses 200 with same id; balance debited once. DB assertion: `SELECT COUNT(*) FROM Transactions WHERE FromAccountId = @a AND IdempotencyKey = @k` = 1.

### DB-level assertions

- Count of transactions per (from_account_id, idempotency_key) ≤ 1 when idempotency_key not null.
- Sum of completed transaction amounts from an account in a test window matches expected balance change.
- No negative balance (if constraint exists) or no test leaves balance negative.

---

## 6. Negative, Boundary & Edge Case Tests

### Auth

- Invalid email format → 400 validation.
- Password too short or weak (if validated) → 400.
- Register with existing email → 400 EMAIL_ALREADY_EXISTS.
- Login with non-existent email → 400 INVALID_CREDENTIALS.
- Expired or invalid JWT → 401.

### KYC

- Missing required field (e.g. identity_card_number) → 400 VALIDATION_FAILED, errors object.
- Invalid date format or future birth_date (if rule) → 400.
- Passport expiry_date in past → allow or reject per product rule.
- Duplicate submission (same type, already pending) → allow or 400 per rule.

### Admin

- Approve user without KYC document → implementation may allow; if not, 400.
- Approve non-existent user id → 404.
- Set account status for non-existent account → 404.
- Customer token on admin endpoint → 403.

### Cards

- Issue card with duplicate number → 400 CARD_NUMBER_EXISTS.
- Issue for non-existent account → 404.
- Expiry_date in past → 400 (validator).
- Revoke non-existent card → 404.

### Transactions

- Amount 0 → 400 validation.
- Amount negative → 400.
- from_account_id not owned by user → 404/403.
- to_account_number not found → 400 DESTINATION_ACCOUNT_NOT_FOUND.
- Account frozen/closed → 400 ACCOUNT_INACTIVE.
- Idempotency-Key missing when required → 400 VALIDATION_FAILED.
- Very large amount (e.g. overflow or max decimal) → reject or handle; no silent corruption.

### Boundaries

- Pagination: pageSize 0 or negative → default or 400; pageNumber 0 or negative → default or 400.
- Expiry_date: today → is_expired true next day (or at midnight per rule).
- Max length for purpose, idempotency_key (e.g. 64) → 400 if exceeded.

### Frozen / closed accounts

- Transfer from frozen account → 400 ACCOUNT_INACTIVE.
- Transfer to closed account → 400 (destination not active).
- GET /api/accounts/me for user with closed account → still returns account with status Closed; POST transfer blocked.

---

## 7. Performance & Load Testing

### Scenarios

- **POST /transactions:** Sustained load (e.g. 50–100 concurrent users), each performing one transfer per 2–5 seconds; run 5–10 minutes. Measure: TPS, p95/p99 latency, error rate. Target: p95 &lt; 2s, error rate &lt; 0.1%.
- **GET /transactions (list):** Many concurrent GETs for same or different accounts; pagination page 1 and deeper. Target: p95 &lt; 500 ms.
- **GET /admin/users:** Admin list with paging under load. Target: p95 &lt; 500 ms.
- **Bulk KYC:** Many concurrent POST /kyc/ic (different users) to test DB and validation throughput.

### Metrics

- Latency: p50, p95, p99.
- Throughput: requests per second (RPS), transactions per second (TPS) for transfer.
- Error rate: 4xx/5xx percentage.
- DB: connection pool usage, slow queries (if logged).

### Tools and data

- **Tools:** k6, NBomber (C#), or JMeter. Scripts: login once per VU, then repeat transfer or list calls.
- **Data:** Seed N users with accounts and balances; use parameterized from_account_id and to_account_number; unique idempotency keys per request to avoid idempotency collapsing all to one transaction.

---

## 8. Security & Vulnerability Testing

### Authentication & authorization

- **Admin only:** Every admin endpoint (e.g. GET/POST /api/admin/users, /api/admin/cards, /api/admin/payroll) called with **customer** token → 403 Forbidden.
- **Customer scope:** GET /api/transactions/account/{id} with account_id of another user → 404 or 403. GET /api/transactions/{id} for another user’s transaction → 404.
- **Unauthenticated:** Any protected endpoint without Bearer token → 401.

### JWT

- **Tampering:** Change one byte in token → 401.
- **Expiry:** Use expired token → 401.
- **Refresh:** Use revoked refresh token → 400 INVALID_REFRESH_TOKEN.
- **Algorithm:** Ensure no "none" or weak alg (handled by default in .NET JWT validation).

### Input validation

- **SQL injection:** Try `' OR 1=1 --` in string fields (email, to_account_number, purpose) → no SQL error; either 400 validation or safe parameterized query.
- **XSS:** Script in purpose or name field → stored and returned encoded (no execution in API; API may not render HTML).

### Sensitive data

- **Responses:** Card list never contains full card number or security code; only masked (e.g. ****1234). KYC endpoints do not return full document content in list responses.
- **Logs:** Search logs for raw password, ScHashed value, full card number → must not appear. Audit log Changes JSON must not contain raw SC or full card number.

### Test cases (summary)

- Unauthorized access to admin endpoints → 403.
- Access other user’s account by guessing account_id → 404/403.
- Access other user’s transaction by id → 404.
- Attempt to see other users’ KYC or cards via enumeration → 404/403.
- Invalid or expired JWT → 401.

### Optional tooling

- **SAST:** Security analyzers (e.g. Roslyn security rules, SecurityCodeScan).
- **Dependency scan:** Check for known vulnerabilities (e.g. OWASP Dependency-Check, Snyk, dotnet list package --vulnerable).
- **DAST:** OWASP ZAP or similar against running API (auth, injection, sensitive data exposure).

---

## 9. Regression Testing & Test Automation Strategy

### CI/CD

- On every **commit/PR:** Run unit tests and integration tests (with test DB). Fail pipeline if any test fails.
- **Before deploy (staging/production):** Run full regression suite (unit + integration + critical E2E); optional performance smoke (e.g. 1 min load); optional security scan.

### Regression suite (must-pass)

- **Unit:** Auth (register, login, duplicate email); KYC submit one type; Admin approve/reject; Account get; Card issue/revoke; Transaction create (success, insufficient funds, idempotency); Payroll due selection and idempotency.
- **Integration:** Register + login + /me; KYC submit + status; Admin list + approve; Issue card + list cards; Create transfer + list transactions; Idempotent replay; Payroll create + run (or mock run).
- **E2E (critical):** E2E-1 (approval flow), E2E-3 (transfer success), E2E-4 (insufficient funds), E2E-5 (blocked when not approved).

### Test data

- **Fixtures:** Seed script or in-memory seed: one admin user, one customer with account, one KYC document, one card, one payroll source account. Use same IDs in tests or resolve by email/role.
- **Factories:** Build user, account, transaction with random but valid data (e.g. Bogus/Faker) for load and variation; avoid hardcoded IDs where possible.
- **Cleanup:** Each integration test uses a transaction rollback or a fresh DB/migration per run; or truncate tables in order (respect FKs) before each suite so tests do not depend on execution order.
- **Isolation:** No test may depend on another test’s side effects; use unique emails, account numbers, idempotency keys per test.

---

## 10. Test Environment & Data Management

### Environments

- **Local dev:** Developer machine; local PostgreSQL or Docker; appsettings.Development.json; run migrations manually or on startup.
- **Test (CI):** Pipeline runs tests; DB = TestContainers PostgreSQL or hosted test DB; connection string from env or secrets; run migrations in pipeline before tests.
- **Staging:** Mirrors production (same stack, smaller scale); seeded with anonymized or synthetic data; used for E2E and performance before release.

### Test data

- **Seed:** Script or DbContext seed: Admin user (known email/password), 2–3 customers (one approved with account, one pending), one account with balance, one PayrollJob with next_run_at in past. Document seed in README or tests.
- **Anonymize:** No real PII in test DB; use clearly fake emails (e.g. admin@test.local, user1@test.local), fake names and document numbers. Do not copy production DB to test without anonymization.
- **Sensitive:** Test KYC documents and card numbers must be synthetic; never use real IDs or card numbers.

### Configuration

- JWT secret and DB connection string in test config or env (different from production).
- Payroll:SourceAccountNumber set to a seeded system account in test so payroll tests can run.

---

## 11. Acceptance Criteria for "Testing Phase Complete"

Use this checklist for sign-off before production.

### Functional

- [ ] All **critical and high** severity bugs from test execution are fixed or accepted with waiver.
- [ ] **Core E2E** scenarios (E2E-1 through E2E-6) pass in test/staging.
- [ ] **Idempotency** tests pass: duplicate POST /transactions with same key returns same result and no double debit.
- [ ] **Concurrency** test: no double spend and no negative balance under parallel transfers.
- [ ] **Regression** suite (unit + integration + critical E2E) is automated and **green** on main branch / release branch.

### Performance

- [ ] **Performance targets** met (e.g. POST /transactions p95 &lt; 2s, GET list p95 &lt; 500 ms) under defined load in staging.
- [ ] No critical performance regressions vs. baseline (if any).

### Security

- [ ] **AuthZ:** Customer cannot access admin endpoints; user cannot access another user’s account/transactions/KYC/cards (tests pass).
- [ ] **Sensitive data:** No card number or security code in API responses or logs (review + automated checks).
- [ ] **Dependency scan** (or equivalent) run; critical/high vulnerabilities addressed or accepted.
- [ ] **JWT** expiry and refresh behavior verified.

### Quality and process

- [ ] **Error responses** consistent: format and codes (e.g. INSUFFICIENT_FUNDS, VALIDATION_FAILED) match Phase 2/3 contract.
- [ ] **Audit** events logged for: approval, KYC verify, card issue/revoke, transfer, payroll run (spot-check or test).
- [ ] **Test data** strategy documented; no production data in test environments.
- [ ] **CI** runs tests on every PR; pipeline is required to pass for merge.

### Sign-off

- [ ] QA lead: Regression and E2E sign-off.
- [ ] Tech lead: Performance and security sign-off.
- [ ] Product: Acceptance criteria for “ready for production” agreed.

---

*Document version: 1.0 – Phase 5 Testing & QA. Use with Phases 1–4 for the full Virtual Banking API lifecycle.*
