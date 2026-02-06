using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Interfaces;

namespace SuperMemo.Infrastructure.Data.Interceptors;

public class AuditLogInterceptor(
    ICurrentUser currentUser,
    IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable or ISoftDeletable)
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        if (entries.Count == 0) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var logs = new List<AuditLog>();
        var timestamp = DateTime.UtcNow;
        var userId = currentUser.Id;

        foreach (var entry in entries)
        {
            var actionName = entry.State.ToString();
            var isSoftDeleteAction = false;

            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable softDeletable })
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = timestamp;
                isSoftDeleteAction = true;
                actionName = "Deleted";
            }

            if (entry.Entity is IAuditable)
            {
                var changes = new Dictionary<string, object>();

                if (isSoftDeleteAction)
                    changes["IsDeleted"] = new { Old = false, New = true };
                else if (entry.State == EntityState.Modified)
                {
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.IsModified)
                            changes[prop.Metadata.Name] = new { Old = prop.OriginalValue, New = prop.CurrentValue };
                    }
                }

                var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue?.ToString() ?? "Unknown";

                logs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    EntityId = entityId,
                    EntityType = entry.Entity.GetType().Name,
                    Action = actionName,
                    Timestamp = timestamp,
                    Changes = System.Text.Json.JsonSerializer.Serialize(changes),
                    UserId = userId
                });
            }
        }

        if (logs.Count != 0)
        {
            var auditSink = serviceProvider.GetRequiredService<IAuditLogSink>();
            await auditSink.WriteLogsAsync(logs, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
