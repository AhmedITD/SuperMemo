using System.Security.Claims;
using Hangfire.Dashboard;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Api.Hangfire;

/// <summary>
/// Allows Hangfire dashboard access for Admin users, or for any request in Development.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var env = httpContext.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        if (env?.IsDevelopment() == true)
            return true;
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.HasClaim(ClaimTypes.Role, UserRole.Admin.ToStringValue());
    }
}
