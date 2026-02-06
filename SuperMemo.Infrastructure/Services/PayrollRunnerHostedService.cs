using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperMemo.Application.Interfaces.Payroll;

namespace SuperMemo.Infrastructure.Services;

public class PayrollRunnerHostedService(IServiceScopeFactory scopeFactory, ILogger<PayrollRunnerHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                using var scope = scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IPayrollRunnerService>();
                var processed = await runner.RunDueJobsAsync(stoppingToken);
                if (processed > 0)
                    logger.LogInformation("Payroll run completed. Processed {Count} job(s).", processed);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payroll run failed.");
            }
        }
    }
}
