using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Profile;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Application.Interfaces.Profile;

namespace SuperMemo.Api.Controllers;

[Authorize]
[Route("api/profile")]
public class ProfileController(IProfileService profileService, ICurrentUser currentUser) : BaseController
{
    [HttpGet]
    public async Task<ActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await profileService.GetProfileAsync(currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await profileService.UpdateProfileAsync(currentUser.Id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
