using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperMemo.Application.Interfaces.Transactions;

namespace SuperMemo.Infrastructure.Services;

/// <summary>
/// Background service that processes pending transactions.
/// Runs every minute.
/// </summary>
public class TransactionProcessingHostedService(IServiceScopeFactory scopeFactory, ILogger<TransactionProcessingHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ITransactionProcessingService>();
                var processed = await processor.ProcessPendingTransactionsAsync(stoppingToken);
                if (processed > 0)
                    logger.LogInformation("Transaction processing completed. Processed {Count} transaction(s).", processed);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction processing failed.");
            }
        }
    }
}

/// <summary>
/// Background service that expires old pending transactions.
/// Runs every hour.
/// </summary>
public class TransactionExpirationHostedService(IServiceScopeFactory scopeFactory, ILogger<TransactionExpirationHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxPendingDuration = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ITransactionProcessingService>();
                var expired = await processor.ExpirePendingTransactionsAsync(MaxPendingDuration, stoppingToken);
                if (expired > 0)
                    logger.LogInformation("Transaction expiration completed. Expired {Count} transaction(s).", expired);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction expiration failed.");
            }
        }
    }
}

/// <summary>
/// Background service that automatically retries failed transactions.
/// Runs every 5 minutes.
/// </summary>
public class TransactionAutoRetryHostedService(IServiceScopeFactory scopeFactory, ILogger<TransactionAutoRetryHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ITransactionProcessingService>();
                var retried = await processor.AutoRetryFailedTransactionsAsync(MaxRetries, stoppingToken);
                if (retried > 0)
                    logger.LogInformation("Transaction auto-retry completed. Retried {Count} transaction(s).", retried);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction auto-retry failed.");
            }
        }
    }
}
