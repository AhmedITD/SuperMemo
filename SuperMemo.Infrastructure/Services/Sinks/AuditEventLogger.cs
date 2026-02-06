using System.Text.Json;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Services.Sinks;

public class AuditEventLogger(ISuperMemoDbContext context, ICurrentUser currentUser) : IAuditEventLogger
{
    public Task LogAsync(string entityType, string entityId, string action, object? changes = null, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Changes = changes != null ? JsonSerializer.Serialize(changes) : "{}",
            Timestamp = DateTime.UtcNow
        };
        context.AuditLogs.Add(log);
        return Task.CompletedTask;
    }
}
