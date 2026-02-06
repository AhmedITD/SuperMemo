# Virtual Banking API – Phase 6: Maintenance & Operations

**Scope:** Backend API (Virtual Banking – Customer Wallet with Linked Cards).  
**Stack:** .NET 8, ASP.NET Core, EF Core, PostgreSQL.

This document is a practical, operations-focused plan for running the system in production.

---

## 1. Operational Objectives & SLIs/SLOs

### Operational goals

| Goal | Description |
|------|-------------|
| **Availability** | API and dependent services (DB, auth) are reachable and responsive for end users and admins. |
| **Reliability** | Transactions complete correctly; no double charges; idempotency and audit trail are intact. |
| **Performance** | Key endpoints respond within acceptable latency so UX is not degraded. |
| **Data integrity** | Balances, transaction history, and KYC data remain consistent; backups are restorable. |
| **Security** | Auth, secrets, and PII/KYC data are protected; access is audited and least-privilege. |

### SLIs (Service Level Indicators)

- **API availability:** % of successful health-check or readiness probes over a window (e.g. 1 min).
- **Request latency:** p50, p95, p99 for key endpoints (see below).
- **Error rate:** % of requests returning 5xx, or % of transaction attempts ending in `failed`.
- **KYC approval time:** Time from KYC submission to admin verification (e.g. median, p95).
- **Payroll success rate:** % of scheduled PayrollJob runs that complete without error (no double-pay, no wrong amount).

### SLOs (targets)

| SLI | Target (example) | Window |
|-----|------------------|--------|
| API availability | ≥ 99.5% | 30 days |
| p95 latency – POST /api/transactions/transfer | ≤ 500 ms | 7 days |
| p95 latency – POST /auth/login | ≤ 300 ms | 7 days |
| p95 latency – GET /api/accounts/me | ≤ 200 ms | 7 days |
| 5xx error rate | ≤ 0.1% | 7 days |
| Transaction failure rate (status = failed) | ≤ 0.5% | 7 days |
| KYC approval median time | ≤ 24 hours | 30 days |
| Payroll run success rate | ≥ 99.9% | 30 days |

Adjust targets based on business and regulatory requirements.

---

## 2. Monitoring, Logging & Alerting

### Metrics to collect (per module)

| Module | Metrics |
|--------|---------|
| **Auth** | Login attempts (total, success, failure); registration count; refresh token usage; JWT validation failures. |
| **KYC** | Submissions per document type (IC, passport, living-identity); pending count; approved/rejected counts; time-in-queue. |
| **Accounts** | Active vs frozen vs closed counts; accounts created per day; balance distribution (buckets) if needed for fraud. |
| **Cards** | Issued, active, expired, revoked; employee vs customer cards; cards expiring in next 30/90 days. |
| **Transactions** | Count by status (Created, Sending, Completed, Failed); total volume (count and amount); idempotent replays (same key returned); latency histogram. |
| **PayrollJobs** | Runs triggered; success/failure/skipped (e.g. account inactive); total amount credited; idempotent replays. |
| **Infrastructure** | DB connections in use, query duration, CPU/memory of API and DB; disk usage. |

### Logging strategy

- **Structured logs:** JSON format with consistent fields: `timestamp`, `level`, `message`, `correlationId` / `requestId`, `userId` (when applicable), `module`/`action`. No card numbers, SC/CVV, or raw KYC document content in logs.
- **INFO:** Registration, login success, KYC submission (type only), admin approval/rejection (user id, outcome), transaction completed (ids, amount, no PII), card issued/revoked (card id, account id), payroll run (job id, outcome).
- **WARN:** Login failure (no password), validation failures, account/card inactive on action, idempotent replay, payroll skipped (reason).
- **ERROR:** 5xx, DB errors, unhandled exceptions, transaction failed after debit (for investigation), payroll run failure.
- **Correlation:** Propagate `X-Correlation-Id` or similar from gateway/API through all layers so one request can be traced across logs.

### Alerting rules

| Condition | Severity | Action |
|-----------|----------|--------|
| API availability &lt; 99% over 5 min | Critical | Page on-call; check health, DB, and deployments. |
| p99 latency &gt; 2 s for POST /transactions | High | Investigate DB and locks; consider throttling. |
| 5xx rate &gt; 1% over 10 min | Critical | Page; check errors and recent deploys. |
| Transaction failure rate &gt; 2% over 15 min | High | Check DB, balance logic, and duplicate keys. |
| Spike in IDEMPOTENT_REPLAY (e.g. 10x baseline) | Medium | Possible client retry storm; verify no double charges. |
| KYC pending queue &gt; N (e.g. 500) for 1 hour | Medium | Alert support/admin; may need more approvers. |
| Payroll run failure (any job failed) | High | Check logs and DB; ensure no partial double-pay. |
| DB connection pool exhausted or &gt; 80% | High | Scale or tune pool; check connection leaks. |
| Repeated failed logins from same IP (e.g. 10 in 5 min) | Medium | Possible brute force; consider rate limit or block. |

Define exact thresholds and escalation (Slack, PagerDuty, etc.) in your alerting tool.

---

## 3. Incident Management & Runbooks

### Incident response process

1. **Detection:** Alerts, user reports, or monitoring anomalies.
2. **Triage:** Assign severity (Critical / High / Medium / Low); assign owner; open incident channel.
3. **Communication:** Notify stakeholders; post status updates; use status page if needed.
4. **Mitigation:** Apply runbooks; implement temporary fixes (e.g. pause payroll, disable feature).
5. **Resolution:** Restore service; verify metrics and key flows.
6. **Postmortem:** Document cause, timeline, and actions; update runbooks and monitoring.

### Runbooks (short)

**Many transactions stuck in `sending` or suddenly failing**

- **Check:** Transaction table for `Status = Sending` count; recent errors in logs; DB locks and long-running queries.
- **Mitigation:** Fix DB or application bug; for stuck rows, consider a one-off script to move to `Failed` and reconcile balances only after careful review. Prefer fixing the root cause before mass updates.

**Increased IDEMPOTENT_REPLAY or suspected double charges**

- **Check:** Logs for duplicate `Idempotency-Key`; DB for duplicate transactions (same from_account_id + idempotency_key); client behavior (retries without backoff).
- **Mitigation:** Confirm idempotency is returning existing transaction (no second debit). If double charges exist, treat as data correction: identify duplicates, reverse, and fix client. Add or tighten client retry policy.

**KYC queue not processed / approvals delayed**

- **Check:** Count of pending KYC documents; admin activity logs; API errors on admin verify endpoints.
- **Mitigation:** Ensure admin capacity; fix API or permission issues; communicate ETA to users.

**PayrollJobs failing or double-paying**

- **Check:** Payroll run logs; failed jobs and error messages; DB for duplicate credits (same idempotency key, same period).
- **Mitigation:** Pause payroll runner if needed; fix bug (e.g. next_run_at, idempotency key); reconcile and correct any double-payments; re-enable after validation.

**DB performance degradation (slow queries on Transactions/Accounts)**

- **Check:** Slow query log; index usage; connection pool; table bloat (e.g. PostgreSQL `pg_stat_user_tables`).
- **Mitigation:** Add or adjust indexes; vacuum/analyze; scale DB or add read replica; optimize heavy queries.

**Authentication errors or high login failure rate**

- **Check:** Auth service logs; JWT config (secret, expiry); DB connectivity; rate of 401/403.
- **Mitigation:** Restart auth dependency if needed; verify secrets and clock skew; add or tighten rate limiting; block abusive IPs if appropriate.

---

## 4. Deployment, Environments & Configuration Management

### Environment strategy

| Env | Purpose | Data | Config / logging |
|-----|---------|------|------------------|
| **Dev** | Feature development | Synthetic or anonymized; reset often | Debug logging; weak secrets; no PII. |
| **Test** | Automated and manual QA | Seeded test data; can be reset | Similar to staging; test doubles for external services. |
| **Staging** | Pre-production validation | Copy or subset of prod (anonymized) | Prod-like config; INFO logging; separate secrets. |
| **Prod** | Live traffic | Real data; backups and retention | INFO/WARN/ERROR only; strong secrets; no debug. |

### Deployment pipeline (CI/CD)

1. **Build:** Compile; run unit tests; package artifact (e.g. Docker image or published app).
2. **Automated tests:** Integration and API tests against Test/Staging; fail pipeline on critical failures.
3. **Staging deployment:** Deploy to staging; run smoke and key E2E flows; optional performance checks.
4. **Promotion:** Manual approval for prod, or automatic with gates (e.g. tests green, no critical alerts).
5. **Rollback:** One-click or scripted rollback to previous version; DB migrations must be backward-compatible so rollback does not require immediate migration revert.

### Configuration and secrets

- **Secrets:** DB connection string, JWT signing key, refresh token secret, any 3rd-party keys: store in a vault (e.g. Azure Key Vault, HashiCorp Vault, or provider-specific secrets manager). Never commit secrets; inject at runtime or via env.
- **Config by environment:** Use appsettings per environment (e.g. `appsettings.Production.json`) or env vars; override only what differs (URLs, feature flags, log level).

### Zero-downtime / low-downtime deployments

- **API:** Deploy new instances; drain old ones (rolling deploy); or blue-green with health checks.
- **DB migrations:** Prefer backward-compatible changes (add column nullable, add index concurrently in PostgreSQL). For breaking changes, use expand-contract: deploy code that supports old and new schema, migrate data, then remove old schema in a later release.
- **Background jobs (e.g. PayrollRunner):** Run in same process or separate worker; ensure only one active runner per job type (e.g. distributed lock or single instance) to avoid double runs.

---

## 5. Database Maintenance & Data Lifecycle

### Ongoing DB maintenance

- **Indexes:** Monitor slow queries; add indexes on filters and joins (e.g. Transactions: from_account_id, created_at, idempotency_key; Accounts: user_id, account_number). Use `EXPLAIN ANALYZE` and avoid over-indexing writes.
- **PostgreSQL:** Schedule `VACUUM (ANALYZE)` regularly; for large tables consider `VACUUM FULL` during maintenance windows. Monitor bloat and table size.
- **Backups:** Full backups at least daily; WAL archiving or continuous backup for point-in-time recovery. Retain per policy (e.g. 30 days). Test restore at least quarterly.

### Data retention and archiving

| Data | Retention (example) | Archive / purge |
|------|---------------------|------------------|
| **Transactions** | 7 years (compliance) | Archive to cold storage after 2 years; purge only after legal hold check. |
| **KYC documents** | Per regulation (e.g. 5–7 years after relationship end) | Archive; secure delete on request (right to erasure) where allowed. |
| **Logs and audit** | 1–2 years hot; then archive | Aggregate and archive; no PII in logs. |
| **PayrollJobs history** | 7 years | Same as transactions; keep run history for audits. |

- **Compliance:** Retain financial and KYC data per local regulations; document retention in a compliance matrix. For GDPR-style requests (access, erasure), implement procedures (anonymize or delete where lawful, and log the action).

---

## 6. Security Operations

### Ongoing security practices

- **Patching:** Regular dependency updates (NuGet, base images); OS and DB security patches on a schedule; critical CVEs patched within SLA.
- **Secrets:** Rotate JWT and DB credentials periodically; use short-lived tokens where possible; no long-term secrets in code or config.
- **Access control:** Review admin and DB access quarterly; least-privilege; MFA for admin and production access.
- **Scans:** SAST in CI; dependency scanning (e.g. for known vulnerabilities); optional DAST or penetration tests for major releases.

### Security monitoring

- **Failed logins:** Alert on high rate per IP or per account; consider lockout and CAPTCHA.
- **Transaction patterns:** Unusual amounts, frequency, or recipients; flag for review (manual or ML later).
- **Admin activity:** Log all admin actions (approvals, KYC verify, card issue/revoke, account status); alert on bulk or anomalous actions.

### Security incident response

- **Suspected account compromise:** Disable account or require re-auth; revoke sessions and cards; notify user; investigate and log.
- **Data leak or breach:** Contain (revoke access, isolate); assess scope; notify affected parties and regulators if required; fix cause and reinforce controls; postmortem.

---

## 7. Capacity Planning & Scalability

### Capacity planning

- **Traffic:** Estimate TPS and concurrent users from product and growth; add headroom (e.g. 2x for peaks).
- **Initial sizing:** Start with 1–2 API instances and a single DB; define connection limits and timeouts.
- **Scaling rules:** Scale API horizontally on CPU or request rate; scale DB vertically first, then read replicas if read-heavy.

### Scaling components

- **API:** Add instances behind load balancer; stateless; session/refresh token in DB or cache.
- **DB:** Read replicas for GET-heavy endpoints (e.g. transaction history); connection pooling; consider partitioning Transactions by date if table grows very large.
- **Background workers:** Scale PayrollRunner instances with a distributed lock so only one runs per job type; scale separately from API if needed.

### Caching

- Cache read-heavy, non-personal data (e.g. reference data) if any. For per-user data (e.g. `/api/transactions/account/{id}`), short TTL or no cache to avoid staleness; use DB and indexes for performance first.

---

## 8. Change Management & Release Governance

### Change management

- **High-risk changes:** Transaction logic, KYC/approval rules, schema changes to financial tables: require design review, tests, and staged rollout.
- **Process:** RFC or ticket for major changes; approval from tech lead or product for money-moving or compliance-related features.

### Feature flags and toggles

- Use feature flags or config toggles for new transaction rules, payroll logic, or risky paths. Enable in staging first, then prod for a subset, then full rollout. Allows quick disable without full rollback.

### Release documentation

- **Per release:** Changelog (user-visible and internal); migration notes (DB, config); rollback steps and known risks. Store in repo or wiki and link from release tag.

---

## 9. Routine Maintenance Tasks & Schedules

| Frequency | Task | Owner | Tools / notes |
|-----------|------|--------|----------------|
| **Daily** | Check critical alerts and overnight failures | DevOps / on-call | Alerting dashboard |
| **Daily** | Log rotation and disk usage | DevOps | Log agent, cron |
| **Weekly** | Review failed transactions and KYC rejections for patterns | Support / product | DB queries, reports |
| **Weekly** | Review alert thresholds and tune | DevOps | Metrics dashboard |
| **Monthly** | DB index and slow query review | DBA / backend | pg_stat, query logs |
| **Monthly** | Payroll reconciliation (runs vs expected) | Finance / product | Reports, DB |
| **Quarterly** | Backup restore test | DevOps | Restore to staging |
| **Quarterly** | Access and secret rotation review | Security / DevOps | IAM, vault |
| **Quarterly** | Dependency and security scan review | Backend / security | NuGet, SAST/SCA |

---

## 10. Operational Dashboards & Reports

### Dashboards (e.g. Grafana, Datadog)

| Dashboard | Contents |
|-----------|----------|
| **Real-time health** | Uptime, latency (p50/p95/p99), error rate, request rate by endpoint. |
| **KYC** | Pending queue size; approval/rejection rate; median time to approval. |
| **Transactions** | Volume (count and amount); status distribution; failure rate; idempotent replays. |
| **Payroll** | Runs per day; success/failure; amount credited; anomalies. |
| **Cards** | Issued, active, expired; expiring soon. |
| **Security** | Failed logins; admin actions; alerts. |

### Regular reports

- **Weekly:** Total transactions (success vs failed); top error codes; KYC pending and resolved; payroll run summary.
- **Monthly:** Same for compliance; capacity and cost summary; incident summary.

Generate from logs and DB; avoid PII in reports; store in secure location.

---

## 11. Exit Criteria for "Maintenance Plan Ready"

Use this checklist to confirm the Maintenance & Operations phase design is complete:

- [ ] **Monitoring:** Critical metrics (availability, latency, error rate, transaction and payroll success) are defined and collected.
- [ ] **Alerting:** Alerts configured for critical and high-severity conditions with assigned owners and escalation.
- [ ] **Logging:** Structured logging (e.g. JSON, correlation ID) in place; no sensitive data (card numbers, SC, raw KYC) in logs.
- [ ] **Runbooks:** Runbooks for major incident types (transactions stuck/failing, idempotency/double charge, KYC backlog, payroll failure, DB performance, auth issues) documented and accessible.
- [ ] **Backup/restore:** Backup schedule and retention defined; restore tested at least once.
- [ ] **Environments:** Dev, Test, Staging, Prod (or minimal viable set) defined with clear config and data rules.
- [ ] **CI/CD:** Pipeline with build, tests, and deploy to staging; promotion and rollback process defined.
- [ ] **Secrets:** Secrets in vault or secure store; not in code; rotation process documented.
- [ ] **DB maintenance:** Index and vacuum strategy; retention and archiving policy for transactions, KYC, logs, payroll.
- [ ] **Security:** Patching, access review, and security monitoring (logins, admin, anomalies) in place; incident response plan for compromise or leak.
- [ ] **Maintenance schedule:** Daily/weekly/monthly/quarterly tasks assigned and documented.
- [ ] **Dashboards:** At least health, transactions, and payroll (and KYC if applicable) dashboards available for ops and support.

Once all items are in place and reviewed, the Maintenance & Operations plan can be considered complete for go-live readiness.
