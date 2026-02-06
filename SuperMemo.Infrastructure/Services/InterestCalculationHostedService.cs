using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperMemo.Application.Interfaces.Accounts;

namespace SuperMemo.Infrastructure.Services;

/// <summary>
/// Background service that calculates and applies interest to Savings accounts.
/// Runs daily at midnight UTC.
/// </summary>
public class InterestCalculationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<InterestCalculationHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait until next midnight UTC
                var now = DateTime.UtcNow;
                var nextMidnight = now.Date.AddDays(1);
                var delay = nextMidnight - now;

                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                using var scope = scopeFactory.CreateScope();
                var interestService = scope.ServiceProvider.GetRequiredService<IInterestCalculationService>();
                var processed = await interestService.ProcessAllSavingsAccountsAsync(stoppingToken);

                if (processed > 0)
                    logger.LogInformation("Interest calculation completed. Processed {Count} account(s).", processed);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Interest calculation failed.");
                // Wait 1 hour before retrying on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
