using SuperMemo.Domain.Entities;

namespace SuperMemo.Application.Interfaces.Sinks;

public interface IAuditLogSink
{
    Task WriteLogsAsync(IEnumerable<AuditLog> logs, CancellationToken cancellationToken = default);
}
