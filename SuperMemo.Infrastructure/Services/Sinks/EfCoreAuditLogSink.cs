using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Services.Sinks;

public class EfCoreAuditLogSink(ISuperMemoDbContext context) : IAuditLogSink
{
    public async Task WriteLogsAsync(IEnumerable<AuditLog> logs, CancellationToken cancellationToken = default)
    {
        await context.AuditLogs.AddRangeAsync(logs, cancellationToken);
    }
}
