namespace SuperMemo.Domain.Interfaces;

public interface IAuditable
{
    int? UpdatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
}
