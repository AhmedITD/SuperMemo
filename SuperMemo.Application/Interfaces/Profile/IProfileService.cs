using SuperMemo.Application.DTOs.requests.Profile;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Profile;

namespace SuperMemo.Application.Interfaces.Profile;

public interface IProfileService
{
    Task<ApiResponse<ProfileResponse>> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProfileResponse>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
