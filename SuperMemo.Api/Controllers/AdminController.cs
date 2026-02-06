using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Admin;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Admin;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Api.Controllers;

[Authorize(Policy = "Admin")]
[Route("api/admin")]
public class AdminController(
    IAdminApprovalService adminApprovalService,
    Application.Interfaces.Admin.IAdminDashboardService adminDashboardService) : BaseController
{
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<PaginatedListResponse<Application.DTOs.responses.Admin.UserApprovalListItemResponse>>>> ListUsers(
        [FromQuery] ApprovalStatus? approvalStatus, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await adminApprovalService.ListUsersByApprovalStatusAsync(approvalStatus, pageNumber, pageSize, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Admin.UserApprovalListItemResponse>>> GetUser(int userId, CancellationToken cancellationToken = default)
    {
        var result = await adminApprovalService.GetUserByIdAsync(userId, cancellationToken);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost("users/{userId}/approval")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveOrRejectUser(int userId, [FromBody] ApproveOrRejectUserRequest request, CancellationToken cancellationToken)
    {
        var result = await adminApprovalService.ApproveOrRejectUserAsync(userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("accounts/{accountId}/status")]
    public async Task<ActionResult<ApiResponse<object>>> SetAccountStatus(int accountId, [FromBody] SetAccountStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await adminApprovalService.SetAccountStatusAsync(accountId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("kyc/ic/{documentId}/verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyIcDocument(int documentId, [FromBody] VerifyKycDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await adminApprovalService.VerifyIcDocumentAsync(documentId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("kyc/passport/{documentId}/verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyPassportDocument(int documentId, [FromBody] VerifyKycDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await adminApprovalService.VerifyPassportDocumentAsync(documentId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("kyc/living-identity/{documentId}/verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyLivingIdentityDocument(int documentId, [FromBody] VerifyKycDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await adminApprovalService.VerifyLivingIdentityDocumentAsync(documentId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("dashboard/metrics")]
    public async Task<ActionResult> GetDashboardMetrics(CancellationToken cancellationToken)
    {
        var result = await adminDashboardService.GetMetricsAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("dashboard/users")]
    public async Task<ActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await adminDashboardService.GetUsersAsync(search, status, page, pageSize, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("dashboard/users/{userId}/status")]
    public async Task<ActionResult> GetUserStatus(int userId, CancellationToken cancellationToken)
    {
        var result = await adminDashboardService.GetUserStatusAsync(userId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
