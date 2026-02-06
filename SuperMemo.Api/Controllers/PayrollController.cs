using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Payroll;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Payroll;

namespace SuperMemo.Api.Controllers;

[Authorize(Policy = "Admin")]
[Route("api/admin/payroll")]
public class PayrollController(IPayrollJobService payrollJobService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedListResponse<Application.DTOs.responses.Payroll.PayrollJobResponse>>>> List(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await payrollJobService.ListAsync(pageNumber, pageSize, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Payroll.PayrollJobResponse>>> Create([FromBody] CreatePayrollJobRequest request, CancellationToken cancellationToken)
    {
        var result = await payrollJobService.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{jobId}")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Payroll.PayrollJobResponse>>> GetById(int jobId, CancellationToken cancellationToken)
    {
        var result = await payrollJobService.GetByIdAsync(jobId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{jobId}")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Payroll.PayrollJobResponse>>> Update(int jobId, [FromBody] UpdatePayrollJobRequest request, CancellationToken cancellationToken)
    {
        var result = await payrollJobService.UpdateAsync(jobId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{jobId}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int jobId, CancellationToken cancellationToken)
    {
        var result = await payrollJobService.DeleteAsync(jobId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
