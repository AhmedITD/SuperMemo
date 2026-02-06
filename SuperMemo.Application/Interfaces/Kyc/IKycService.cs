using SuperMemo.Application.DTOs.requests.Kyc;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Kyc;

namespace SuperMemo.Application.Interfaces.Kyc;

public interface IKycService
{
    Task<ApiResponse<int>> SubmitIcDocumentAsync(SubmitIcDocumentRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> SubmitPassportDocumentAsync(SubmitPassportDocumentRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> SubmitLivingIdentityDocumentAsync(SubmitLivingIdentityDocumentRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<KycStatusResponse>> GetStatusAsync(int userId, CancellationToken cancellationToken = default);
}
