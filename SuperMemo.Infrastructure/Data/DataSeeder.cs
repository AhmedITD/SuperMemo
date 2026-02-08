using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Infrastructure.Data;

/// <summary>
/// Seeds the database with initial data (admin user, optional test customer with account and card).
/// Idempotent: skips seeding if data already exists.
/// </summary>
public class DataSeeder
{
    public const string AdminPhone = "00000000000";
    public const string AdminDefaultPassword = "Admin@123";
    public const string TestCustomerPhone = "11111111111";
    public const string TestCustomerPassword = "Customer@123";

    private readonly SuperMemoDbContext _db;
    private readonly IPasswordService _passwordService;

    public DataSeeder(SuperMemoDbContext db, IPasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminAsync(cancellationToken);
        await SeedTestCustomerAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        if (await _db.Users.AnyAsync(u => u.Phone == AdminPhone, cancellationToken))
            return;

        var admin = new User
        {
            FullName = "System Admin",
            Phone = AdminPhone,
            PasswordHash = _passwordService.HashPassword(AdminDefaultPassword),
            Role = UserRole.Admin,
            ApprovalStatus = ApprovalStatus.Approved,
            KycStatus = KycStatus.Verified,
            KybStatus = KybStatus.Verified
        };
        _db.Users.Add(admin);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTestCustomerAsync(CancellationToken cancellationToken)
    {
        if (await _db.Users.AnyAsync(u => u.Phone == TestCustomerPhone, cancellationToken))
            return;

        var customer = new User
        {
            FullName = "Test Customer",
            Phone = TestCustomerPhone,
            PasswordHash = _passwordService.HashPassword(TestCustomerPassword),
            Role = UserRole.Customer,
            ApprovalStatus = ApprovalStatus.Approved,
            KycStatus = KycStatus.Verified,
            KybStatus = KybStatus.Verified
        };
        _db.Users.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        var accountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken);
        var account = new Account
        {
            UserId = customer.Id,
            Balance = 1000.00m,
            Currency = "USD",
            Status = AccountStatus.Active,
            AccountNumber = accountNumber,
            AccountType = AccountType.Regular
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        var cardNumber = await GenerateUniqueCardNumberAsync(cancellationToken);
        var expiry = DateTime.UtcNow.Date.AddYears(5);
        var card = new Card
        {
            AccountId = account.Id,
            Number = cardNumber,
            Type = CardType.Virtual,
            ExpiryDate = expiry,
            ScHashed = _passwordService.HashPassword("123"),
            IsActive = true,
            IsExpired = expiry <= DateTime.UtcNow.Date,
            IsEmployeeCard = false
        };
        _db.Cards.Add(card);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            var number = "SM" + Random.Shared.Next(100000000, 999999999).ToString();
            if (!await _db.Accounts.AnyAsync(a => a.AccountNumber == number, cancellationToken))
                return number;
        }
        return "SM" + Guid.NewGuid().ToString("N")[..10];
    }

    private async Task<string> GenerateUniqueCardNumberAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var number = string.Create(16, Random.Shared, (span, r) =>
            {
                span[0] = (char)('0' + r.Next(1, 10));
                for (int i = 1; i < 16; i++)
                    span[i] = (char)('0' + r.Next(0, 10));
            });
            if (!await _db.Cards.AnyAsync(c => c.Number == number, cancellationToken))
                return number;
        }
        throw new InvalidOperationException("Could not generate unique card number for seed.");
    }
}
