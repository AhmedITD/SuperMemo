# Virtual Banking API – Customer Wallet with Linked Cards  
## Phase 1 – Planning Document

---

## 1. Refined Problem Statement & Vision

**Problem statement**  
We need a secure, compliant virtual wallet backend where each customer has a single virtual account that can hold multiple linked cards (virtual, VisaCard, MasterCard). Users must prove identity via KYC (and optionally KYB), be approved by an admin before using the wallet, and perform safe, non-duplicate transactions. Optionally, approved employees can receive recurring salary deposits into their wallet.

**Vision**  
A regulated, admin-controlled virtual banking API that: (1) onboards users through verified identity, (2) gives each approved user one wallet (account) with balance and currency, (3) allows admins to issue and manage multiple card types per account, (4) supports idempotent peer-to-account transfers, and (5) optionally automates salary deposits for employees.

**Key objectives (bullets)**  
- One virtual account per approved customer with balance and currency.  
- Multiple cards per account (virtual, MasterCard, VisaCard) with secure storage (e.g. hashed security code).  
- KYC/KYB submission and admin-driven approval before account activation.  
- Idempotent transactions to prevent duplicate sends and ensure auditability.  
- Clear transaction lifecycle: Created → sending → completed | failed.  
- Optional payroll integration for monthly salary into employee-linked accounts/cards.  
- Role-based access: customers vs admins, with clear separation of duties.  
- Audit trail and compliance-ready handling of identity and financial data.

---

## 2. Stakeholders & Needs

| Stakeholder | Top needs | Pain points |
|-------------|-----------|-------------|
| **Customers** | Fast onboarding, one wallet, multiple cards, reliable transfers, salary receipt (if employee). | KYC friction, approval delays, failed or duplicate transactions, unclear status. |
| **Admins** | KYC/KYB review, approve/reject users and accounts, issue/revoke cards, monitor transactions. | Manual review load, no bulk actions, unclear audit trail. |
| **Compliance / Regulators** | KYC/KYB evidence, audit logs, data retention, secure handling of identity and card data. | Gaps in documentation, weak access controls, unclear data handling. |
| **Employers** | Reliable salary disbursement to employee accounts, simple configuration, visibility. | Wrong account, failed runs, no clear status of payroll jobs. |
| **Engineering team** | Clear APIs, idempotency, testability, maintainable data model. | Ambiguous flows, missing idempotency, hard-to-test approval and payroll. |
| **Support** | User/account status, transaction history, card status, payroll job status. | No single view of user journey, difficult troubleshooting. |

---

## 3. In-Scope vs Out-of-Scope (MVP)

### In scope for MVP (v1)

- **User registration** with role (customer/admin).  
- **KYC**: One document type per user (Identity Card **or** Passport **or** Living Identity); status (e.g. pending/verified/rejected).  
- **KYB**: Optional; same status model if present.  
- **Admin approval**: Approve/reject user and account; account status (e.g. pending_approval | active | frozen | closed).  
- **Single account per user** with balance and one currency (e.g. single-currency wallet).  
- **Cards**: Admin-issued; types virtual | MasterCard | VisaCard; number, expiry, hashed security code, active/expired, optional `is_employee_card`.  
- **Transactions**: Send from account to account (by account number); status flow Created → sending → completed | failed; purpose/description; **idempotency by `idempotency_key`**.  
- **Payroll (optional MVP)**: Basic PayrollJobs (employee, amount, currency, schedule, next_run_at, status); create salary credit transactions into employee account.  
- **Auth & RBAC**: Customer vs admin; protect admin and sensitive endpoints.  
- **Audit**: Essential audit events for KYC, approval, cards, and transactions.

### Out of scope / later

- **Advanced fraud detection** (ML, anomaly scoring).  
- **External bank transfers** (outbound to external banks).  
- **Multi-currency** accounts and FX.  
- **Advanced payroll**: Tax withholding, multiple employers, complex schedules, approval workflows.  
- **Card network integration** (real Visa/Mastercard processing).  
- **Full KYB flows** (company verification, documents) if not needed for v1.  
- **Chargebacks, disputes, refunds** as first-class flows.  
- **Real-time notifications** (push/email) – can be added after MVP.

---

## 4. High-Level Features by Module

### Users
- Register with name, phone, role (customer/admin).  
- Profile view and update (within policy).  
- Status reflects KYC/KYB and approval (pending_approval | approved | rejected).  
- Only approved customers can use wallet and cards.

### KYC/KYB
- Submit one of: Identity Card, Passport, or Living Identity document; store with status.  
- Optional KYB submission and status for business users.  
- Admin can verify/reject documents; status drives approval eligibility.  
- Documents and status are immutable/versioned for audit.

### Admin approval
- List users/accounts by approval status (e.g. pending_approval).  
- Approve or reject user and associated account.  
- Set account status: active, frozen, closed.  
- Approval gates: account becomes usable only when approved and active.

### Accounts
- One account per approved user; balance and currency.  
- Balance updates only via transactions (debit/credit).  
- Status: pending_approval | active | frozen | closed; only active can transact.  
- Get account by user (customer) or by id/number (admin).

### Cards
- Admin issues cards for an account: type (virtual | MasterCard | VisaCard).  
- Store: unique number, expiry, hashed security code (never plain); is_active, is_expired, is_employee_card.  
- List cards per account; revoke/deactivate card (is_active = false or is_expired = true).  
- Card number/identifier used for linking (e.g. payroll) and display only where authorised.

### Transactions
- Create transfer: from_account_id, to_account_number, amount, purpose/description, **idempotency_key**.  
- Status flow: Created → sending → completed or failed.  
- Prevent duplicate processing using idempotency_key (return same result for same key).  
- List transactions per account with filters (date, status).  
- Sufficient balance and account status checks before completing.

### Idempotency
- Client sends idempotency_key on create transfer.  
- Store and enforce uniqueness per (e.g.) user or account scope; return existing response for replay.  
- Key validity window and cleanup policy (e.g. 24h or 7 days) to avoid unbounded growth.

### Payroll
- CRUD for PayrollJobs: employee_user_id, employer reference, amount, currency, schedule, next_run_at, status (active | paused | canceled).  
- Scheduled job (e.g. monthly) creates salary credit transaction to employee’s account.  
- Optional link to is_employee_card for reporting/visibility.  
- Idempotent payroll run (e.g. per job + period) to avoid duplicate salary credits.

---

## 5. Risks & Constraints

| # | Risk | Why it matters | Mitigation |
|---|------|----------------|------------|
| 1 | **KYC/KYB data breach** | Identity documents are highly sensitive; breach = regulatory and reputational damage. | Encrypt at rest and in transit; strict access control; audit all access; minimal retention and secure deletion. |
| 2 | **Card data exposure** | Card number + security code are payment-sensitive; exposure enables fraud. | Never store raw security code; store only strong hash (e.g. bcrypt/Argon2); mask card number in logs and responses. |
| 3 | **Duplicate transactions** | Double spend or double credit if idempotency is missing or weak. | Enforce idempotency_key on transfer creation; same key returns same response; use DB constraint or dedicated store. |
| 4 | **Concurrent balance updates** | Race conditions can corrupt balance or allow overdraft. | Use DB-level locking (e.g. row lock on account) or optimistic concurrency; balance change in single transactional unit. |
| 5 | **Approval bypass** | Unapproved users could transact if checks are missing. | Enforce approval_status and account status on every transaction and card use; centralise checks in service layer. |
| 6 | **Admin abuse** | Admins can issue cards, approve accounts, see PII. | Role-based access; audit all admin actions; least privilege; separate roles if needed (e.g. support vs super-admin). |
| 7 | **Payroll run failures** | Failed or partial runs cause wrong or missing salary. | Idempotent runs per job+period; retry with same idempotency; alerting; status (active/paused/canceled) and next_run_at. |
| 8 | **Compliance gaps** | Regulators require clear KYC, approval, and transaction trail. | Immutable audit log for KYC, approval, cards, transactions; retention policy; document data handling in privacy/security docs. |
| 9 | **Weak input validation** | Invalid amounts, account numbers, or keys cause errors or security issues. | Validate amount > 0, account exists and is active; validate idempotency_key format and length; reject invalid data early. |
| 10 | **Operational visibility** | Hard to detect issues (e.g. stuck transactions, payroll not running). | Log transaction state changes; monitor failed transactions and payroll job status; health checks and alerts. |

**Extra notes**  
- **KYC/KYB**: Treat document content as PII; encrypt; access only for review and compliance; log who accessed what.  
- **Card data**: Security code hashed with strong algorithm and salt; card number stored only if required, else tokenised/masked.  
- **Transaction integrity**: Single transaction boundary for balance updates and transaction record; idempotency key stored and checked before any write.

---

## 6. Assumptions & Open Questions

### Assumptions
- One currency per account (or per system) for MVP.  
- One account per user; no multi-account per user in v1.  
- Employers (or payroll operator) already know employee user/account identifiers for PayrollJobs.  
- KYC = one document type per user (IC or Passport or Living Identity); no "multiple documents" in MVP.  
- Admin approval is manual; no auto-approval based on rules in MVP.  
- Card "type" (virtual/MasterCard/VisaCard) is metadata only in MVP; no real card network integration.  
- All transfers are internal (account-to-account within the system).  
- Idempotency key is provided by the client (e.g. UUID); server does not generate it.  
- Timezone for "monthly" payroll is agreed (e.g. UTC or organisation default).

### Open questions
- **Approval SLA**: Target time for admin to approve/reject (e.g. 24h, 48h)?  
- **Salary schedule**: Exact rule for "monthly" (e.g. 1st of month, last working day, configurable per employer)?  
- **Limits**: Max transaction amount, daily/monthly limits per account or per user?  
- **Idempotency key scope**: Per user, per account, or global? Key TTL (e.g. 24h vs 7 days)?  
- **Card number format**: Algorithm for generating "unique number" (e.g. BIN + random; any format constraints)?  
- **Frozen/closed accounts**: Can balance be withdrawn or transferred out when frozen/closed?  
- **KYB**: Required for v1 or fully optional? If optional, which features depend on it?  
- **Employer entity**: Is "employer" a separate table/entity or just an identifier in PayrollJobs?

---

## 7. Initial Roadmap

| Phase | Duration | Focus | Deliverables |
|-------|----------|--------|--------------|
| **Phase 1 – Foundation & compliance** | ~3–4 weeks | Registration, KYC, admin approval, accounts | User registration and auth (customer/admin). KYC document submission (IC, Passport, Living Identity) and status. Admin review and approve/reject user and account. Create account on approval; account status (pending_approval, active, frozen, closed). Basic RBAC and audit logging for these actions. |
| **Phase 2 – Core wallet & transactions** | ~3–4 weeks | Transactions, idempotency, balance integrity | Create transfer API (from_account_id, to_account_number, amount, purpose, idempotency_key). Status flow Created → sending → completed/failed. Idempotency store and enforcement. Balance update in same transaction; concurrency control. List transactions; balance and account checks. |
| **Phase 3 – Cards & payroll** | ~3–4 weeks | Cards, optional payroll | Admin: issue cards (virtual, MasterCard, VisaCard); store number, expiry, hashed security code, is_active, is_expired, is_employee_card. List/revoke cards per account. PayrollJobs: create/update/delete; scheduler (e.g. monthly) to create salary credit transactions; idempotent per job+period; link to employee account (and optionally is_employee_card). |

**Total**: ~8–12 weeks for MVP.  
**Suggested order**: Phase 1 → Phase 2 → Phase 3; Phase 3 can start in parallel with hardening of Phase 2 (e.g. monitoring, load tests) if the team is split.

---

*Document version: 1.0 – Phase 1 Planning. Use this to derive backlog items, API contracts, and technical design docs.*
