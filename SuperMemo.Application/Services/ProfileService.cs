using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.requests.Profile;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Profile;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Profile;
using SuperMemo.Application.Interfaces.Sinks;

namespace SuperMemo.Application.Services;

public class ProfileService(
    ISuperMemoDbContext db,
    IAuditEventLogger auditLogger) : IProfileService
{
    public async Task<ApiResponse<ProfileResponse>> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return ApiResponse<ProfileResponse>.ErrorResponse("User not found.", code: ErrorCodes.ResourceNotFound);

        var response = new ProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Phone = user.Phone,
            ImageUrl = user.ImageUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return ApiResponse<ProfileResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<ProfileResponse>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return ApiResponse<ProfileResponse>.ErrorResponse("User not found.", code: ErrorCodes.ResourceNotFound);

        var changes = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            if (request.FullName.Length < 2 || request.FullName.Length > 200)
                return ApiResponse<ProfileResponse>.ErrorResponse("Full name must be between 2 and 200 characters.", code: ErrorCodes.ValidationFailed);

            changes["FullName"] = new { Old = user.FullName, New = request.FullName };
            user.FullName = request.FullName;
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            // Basic phone validation (can be enhanced)
            if (request.Phone.Length < 10 || request.Phone.Length > 20)
                return ApiResponse<ProfileResponse>.ErrorResponse("Phone number must be between 10 and 20 characters.", code: ErrorCodes.ValidationFailed);

            changes["Phone"] = new { Old = user.Phone, New = request.Phone };
            user.Phone = request.Phone;
        }

        if (!string.IsNullOrWhiteSpace(request.ProfileImageUrl))
        {
            changes["ImageUrl"] = new { Old = user.ImageUrl, New = request.ProfileImageUrl };
            user.ImageUrl = request.ProfileImageUrl;
        }

        if (changes.Count == 0)
            return ApiResponse<ProfileResponse>.ErrorResponse("No fields to update.", code: ErrorCodes.ValidationFailed);

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync("User", user.Id.ToString(), "ProfileUpdated", changes, cancellationToken);

        var response = new ProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Phone = user.Phone,
            ImageUrl = user.ImageUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return ApiResponse<ProfileResponse>.SuccessResponse(response);
    }
}
