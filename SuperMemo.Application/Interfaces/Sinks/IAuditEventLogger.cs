namespace SuperMemo.Application.Interfaces.Sinks;

/// <summary>
/// Logs explicit audit events for critical actions (Phase 3 design: registration, KYC, approval, card, transaction, payroll).
/// </summary>
public interface IAuditEventLogger
{
    Task LogAsync(string entityType, string entityId, string action, object? changes = null, CancellationToken cancellationToken = default);
}
