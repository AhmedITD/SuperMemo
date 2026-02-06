using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Infrastructure.Services.Auth;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public int Id
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(sub, out var id) ? id : 0;
        }
    }

    public UserRole Role
    {
        get
        {
            var roleClaim = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(roleClaim, true, out var role) ? role : UserRole.Customer;
        }
    }
}
