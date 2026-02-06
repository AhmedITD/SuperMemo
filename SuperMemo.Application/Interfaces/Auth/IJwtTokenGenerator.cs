using SuperMemo.Domain.Entities;

namespace SuperMemo.Application.Interfaces.Auth;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
