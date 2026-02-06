namespace SuperMemo.Application.Interfaces.Payroll;

/// <summary>
/// Runs due payroll jobs: creates salary credit transactions and advances next_run_at (Phase 3 design).
/// </summary>
public interface IPayrollRunnerService
{
    Task<int> RunDueJobsAsync(CancellationToken cancellationToken = default);
}
