# SuperMemo – Virtual Banking API

.NET 8 backend: customer wallet, KYC/KYB, admin approval, cards, transactions (idempotent), optional payroll.  
Stack: ASP.NET Core, EF Core, PostgreSQL, JWT, Otpiq OTP.

## Push to a remote

1. Create a new **empty** repository on GitHub (or GitLab / Azure DevOps).
2. In this folder run:

```bash
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO.git
git push -u origin main
```

Use your repo URL and SSH if you prefer (`git@github.com:USER/REPO.git`).

## Run locally

- Set `ConnectionStrings:DefaultConnection` and `Otpiq:ApiKey` in `SuperMemo.Api/appsettings.json` or User Secrets.
- From solution root: `dotnet ef database update --project SuperMemo.Infrastructure\SuperMemo.Infrastructure.csproj --startup-project SuperMemo.Api\SuperMemo.Api.csproj`
- `dotnet run --project SuperMemo.Api`
