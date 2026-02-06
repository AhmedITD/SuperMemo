using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.Interfaces.Analytics;
using SuperMemo.Application.Interfaces.Auth;

namespace SuperMemo.Api.Controllers;

[Authorize]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService, ICurrentUser currentUser) : BaseController
{
    [HttpGet("overview")]
    public async Task<ActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await analyticsService.GetOverviewAsync(currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult> GetTransactions(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? direction,
        CancellationToken cancellationToken)
    {
        var result = await analyticsService.GetTransactionsAsync(currentUser.Id, startDate, endDate, direction, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("balance-trend")]
    public async Task<ActionResult> GetBalanceTrend(CancellationToken cancellationToken)
    {
        var result = await analyticsService.GetBalanceTrendAsync(currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("transactions-list")]
    public async Task<ActionResult> GetTransactionsList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await analyticsService.GetTransactionsListAsync(currentUser.Id, page, pageSize, startDate, endDate, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
