# SuperMemo API Integration Tests

Tests all backend API endpoints using `WebApplicationFactory` (in-process server).

## Prerequisites

- **PostgreSQL** running with the SuperMemo database (same as API).
- **Seeded data**: Run the API once with `SeedDatabase: true` (or use Development) so that test users exist:
  - **Customer:** phone `11111111111`, password `Customer@123`
  - **Admin:** phone `00000000000`, password `Admin@123`

## Run tests

From the solution root:

```bash
dotnet test SuperMemo.Tests/SuperMemo.Tests.csproj
```

Or from Visual Studio / Rider: run all tests in the SuperMemo.Tests project.

## What is tested

- **Auth:** login (valid/invalid), refresh, Me (unauthorized), send-verification / forgot-password / reset-password not covered by these tests.
- **Accounts:** GET /api/accounts/me (with token, without token).
- **User cards:** GET/POST /api/user/cards (with customer token).
- **Profile:** GET/PUT /api/profile (with customer token).
- **Transactions:** GET by account, POST transfer (with customer token).
- **Dashboard:** GET /api/dashboard (with customer token).
- **Analytics:** overview, balance-trend, transactions, transactions-list (with customer token).
- **KYC:** GET /api/kyc/status (with customer token).
- **Payments:** POST /api/payments/top-up (with customer token).
- **Admin:** GET /api/admin/users, dashboard/metrics, dashboard/users, payroll list, transactions/risk-review (with admin token); GET /api/admin/users with customer token returns 403.
