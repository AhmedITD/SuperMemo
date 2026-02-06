using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperMemo.Application.Interfaces.Accounts;

namespace SuperMemo.Infrastructure.Services;

/// <summary>
/// Background service that resets daily spending limits for Savings accounts.
/// Runs daily at midnight UTC.
/// </summary>
public class DailyLimitResetHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyLimitResetHostedService> logger) : BackgroundService
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
                var dailyLimitService = scope.ServiceProvider.GetRequiredService<IDailyLimitService>();
                var reset = await dailyLimitService.ResetAllDailyLimitsAsync(stoppingToken);

                if (reset > 0)
                    logger.LogInformation("Daily limit reset completed. Reset {Count} account(s).", reset);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Daily limit reset failed.");
                // Wait 1 hour before retrying on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
