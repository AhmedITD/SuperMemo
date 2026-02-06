using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.Interfaces.Dashboard;
using SuperMemo.Application.Interfaces.Auth;

namespace SuperMemo.Api.Controllers;

[Authorize]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboardService, ICurrentUser currentUser) : BaseController
{
    [HttpGet]
    public async Task<ActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetDashboardAsync(currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
