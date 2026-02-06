using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Interfaces.Auth;

public interface ICurrentUser
{
    int Id { get; }
    UserRole Role { get; }
}
