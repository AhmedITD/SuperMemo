using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Payroll;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Application.Interfaces.Transactions;
using SuperMemo.Infrastructure.Options;

namespace SuperMemo.Infrastructure.Services;

public class PayrollRunnerService(
    ISuperMemoDbContext db,
    ITransactionService transactionService,
    IAuditEventLogger auditLogger,
    IOptions<PayrollOptions> options) : IPayrollRunnerService
{
    public async Task<int> RunDueJobsAsync(CancellationToken cancellationToken = default)
    {
        var sourceAccountNumber = options.Value.SourceAccountNumber;
        if (string.IsNullOrWhiteSpace(sourceAccountNumber))
            return 0;

        var sourceAccount = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == sourceAccountNumber, cancellationToken);
        if (sourceAccount == null)
            return 0;

        var now = DateTime.UtcNow;
        var dueJobs = await db.PayrollJobs
            .Include(j => j.EmployeeUser)
            .Where(j => j.Status == SuperMemo.Domain.Enums.PayrollJobStatus.Active && j.NextRunAt != null && j.NextRunAt <= now)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var job in dueJobs)
        {
            var employeeAccount = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == job.EmployeeUserId, cancellationToken);
            if (employeeAccount == null)
                continue;

            var period = job.NextRunAt?.ToString("yyyyMM") ?? now.ToString("yyyyMM");
            var idempotencyKey = $"payroll_{job.Id}_{period}";
            var result = await transactionService.CreatePayrollCreditAsync(
                sourceAccount.Id,
                employeeAccount.AccountNumber,
                job.Amount,
                idempotencyKey,
                $"Payroll {period}",
                cancellationToken);

            if (!result.Success)
                continue;

            job.NextRunAt = (job.NextRunAt ?? now).AddMonths(1);
            await db.SaveChangesAsync(cancellationToken);
            await auditLogger.LogAsync("PayrollJob", job.Id.ToString(), "PayrollRun", new { job.EmployeeUserId, period, result.Data?.Id }, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            processed++;
        }

        return processed;
    }
}
