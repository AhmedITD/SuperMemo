using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Kyc;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Kyc;

namespace SuperMemo.Api.Controllers;

[Authorize]
[Route("api/kyc")]
public class KycController(IKycService kycService, Application.Interfaces.Auth.ICurrentUser currentUser) : BaseController
{
    [HttpPost("ic")]
    public async Task<ActionResult<ApiResponse<int>>> SubmitIcDocument([FromBody] SubmitIcDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await kycService.SubmitIcDocumentAsync(request, currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("passport")]
    public async Task<ActionResult<ApiResponse<int>>> SubmitPassportDocument([FromBody] SubmitPassportDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await kycService.SubmitPassportDocumentAsync(request, currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("living-identity")]
    public async Task<ActionResult<ApiResponse<int>>> SubmitLivingIdentityDocument([FromBody] SubmitLivingIdentityDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await kycService.SubmitLivingIdentityDocumentAsync(request, currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Kyc.KycStatusResponse>>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await kycService.GetStatusAsync(currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
