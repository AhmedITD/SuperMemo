using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Interfaces;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Interfaces;

namespace SuperMemo.Infrastructure.Data;

public class SuperMemoDbContext : DbContext, ISuperMemoDbContext
{
    public SuperMemoDbContext(DbContextOptions<SuperMemoDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<IcDocument> IcDocuments { get; set; }
    public DbSet<PassportDocument> PassportDocuments { get; set; }
    public DbSet<LivingIdentityDocument> LivingIdentityDocuments { get; set; }
    public DbSet<PayrollJob> PayrollJobs { get; set; }
    public DbSet<PhoneVerificationCode> PhoneVerificationCodes { get; set; }
    
    // Advanced features entities
    public DbSet<MerchantAccount> MerchantAccounts { get; set; }
    public DbSet<FraudDetectionRule> FraudDetectionRules { get; set; }
    public DbSet<TransactionStatusHistory> TransactionStatusHistory { get; set; }
    
    // Payment gateway entities
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentWebhookLog> PaymentWebhookLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SuperMemoDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;
            modelBuilder.Entity(entityType.ClrType)
                .Property("IsDeleted")
                .HasDefaultValue(false);

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var falseConstant = Expression.Constant(false);
            var binaryExpression = Expression.Equal(property, falseConstant);
            var filter = Expression.Lambda(binaryExpression, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);

            foreach (var index in entityType.GetIndexes())
            {
                if (string.IsNullOrEmpty(index.GetFilter()))
                    index.SetFilter("\"IsDeleted\" = false");
            }
        }
    }
}
