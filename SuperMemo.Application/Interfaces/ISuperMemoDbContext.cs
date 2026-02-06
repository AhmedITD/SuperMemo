using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Application.Interfaces;

public interface ISuperMemoDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<RefreshToken> RefreshTokens { get; set; }
    DbSet<AuditLog> AuditLogs { get; set; }
    DbSet<Account> Accounts { get; set; }
    DbSet<Card> Cards { get; set; }
    DbSet<Transaction> Transactions { get; set; }
    DbSet<IcDocument> IcDocuments { get; set; }
    DbSet<PassportDocument> PassportDocuments { get; set; }
    DbSet<LivingIdentityDocument> LivingIdentityDocuments { get; set; }
    DbSet<PayrollJob> PayrollJobs { get; set; }
    DbSet<PhoneVerificationCode> PhoneVerificationCodes { get; set; }
    
    // Advanced features entities
    DbSet<MerchantAccount> MerchantAccounts { get; set; }
    DbSet<FraudDetectionRule> FraudDetectionRules { get; set; }
    DbSet<TransactionStatusHistory> TransactionStatusHistory { get; set; }
    
    // Phase 9 - Payment Gateway entities
    DbSet<Payment> Payments { get; set; }
    DbSet<PaymentWebhookLog> PaymentWebhookLogs { get; set; }
    
    DatabaseFacade Database { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
