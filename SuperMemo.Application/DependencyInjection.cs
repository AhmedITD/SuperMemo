using FluentValidation;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using SuperMemo.Application.Interfaces.Accounts;
using SuperMemo.Application.Interfaces.Admin;
using SuperMemo.Application.Interfaces.Analytics;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Application.Interfaces.Cards;
using SuperMemo.Application.Interfaces.Dashboard;
using SuperMemo.Application.Interfaces.Fraud;
using SuperMemo.Application.Interfaces.Kyc;
using SuperMemo.Application.Interfaces.Payments;
using SuperMemo.Application.Interfaces.Payroll;
using SuperMemo.Application.Interfaces.Profile;
using SuperMemo.Application.Interfaces.Transactions;
using SuperMemo.Application.Services;

namespace SuperMemo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IKycService, KycService>();
        services.AddScoped<IAdminApprovalService, AdminApprovalService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IPayrollJobService, PayrollJobService>();
        
        // Advanced features services
        services.AddScoped<ITransactionStatusMachine, TransactionStatusMachine>();
        services.AddScoped<IFraudDetectionService, FraudDetectionService>();
        services.AddScoped<IFailureClassificationService, FailureClassificationService>();
        services.AddScoped<IPaymentInitiationService, PaymentInitiationService>();
        services.AddScoped<ITransactionProcessingService, TransactionProcessingService>();
        
        // Phase 8 services
        services.AddScoped<IAccountTypeService, AccountTypeService>();
        services.AddScoped<IInterestCalculationService, InterestCalculationService>();
        services.AddScoped<IDailyLimitService, DailyLimitService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
