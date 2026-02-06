using System.ComponentModel.DataAnnotations;

namespace SuperMemo.Domain.Entities;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; }
    public int? UserId { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public required string Action { get; set; }
    public required string Changes { get; set; }
    public DateTime Timestamp { get; set; }
}
