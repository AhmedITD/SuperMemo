using SuperMemo.Application.DTOs.requests.Admin;
using SuperMemo.Application.DTOs.responses.Admin;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Interfaces.Admin;

public interface IAdminApprovalService
{
    Task<ApiResponse<PaginatedListResponse<UserApprovalListItemResponse>>> ListUsersByApprovalStatusAsync(ApprovalStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserApprovalListItemResponse>> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> ApproveOrRejectUserAsync(int userId, ApproveOrRejectUserRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> SetAccountStatusAsync(int accountId, SetAccountStatusRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> VerifyIcDocumentAsync(int documentId, VerifyKycDocumentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> VerifyPassportDocumentAsync(int documentId, VerifyKycDocumentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> VerifyLivingIdentityDocumentAsync(int documentId, VerifyKycDocumentRequest request, CancellationToken cancellationToken = default);
}
