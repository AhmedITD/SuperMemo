using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.DTOs.requests.Admin;
using SuperMemo.Application.DTOs.responses.Admin;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Admin;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class AdminApprovalService(ISuperMemoDbContext db, IAuditEventLogger auditLogger) : IAdminApprovalService
{
    public async Task<ApiResponse<PaginatedListResponse<UserApprovalListItemResponse>>> ListUsersByApprovalStatusAsync(ApprovalStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.Users
            .Where(u => u.Role == UserRole.Customer)
            .Include(u => u.Account)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(u => u.ApprovalStatus == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserApprovalListItemResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Phone = u.Phone,
                KycStatus = u.KycStatus,
                KybStatus = u.KybStatus,
                ApprovalStatus = u.ApprovalStatus,
                AccountId = u.Account != null ? u.Account.Id : null,
                AccountNumber = u.Account != null ? u.Account.AccountNumber : null,
                AccountStatus = u.Account != null ? u.Account.Status : null
            })
            .ToListAsync(cancellationToken);

        var response = new PaginatedListResponse<UserApprovalListItemResponse>(users, total, pageNumber, pageSize);
        return ApiResponse<PaginatedListResponse<UserApprovalListItemResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<UserApprovalListItemResponse>> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Where(u => u.Role == UserRole.Customer && u.Id == userId)
            .Include(u => u.Account)
            .Select(u => new UserApprovalListItemResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Phone = u.Phone,
                KycStatus = u.KycStatus,
                KybStatus = u.KybStatus,
                ApprovalStatus = u.ApprovalStatus,
                AccountId = u.Account != null ? u.Account.Id : null,
                AccountNumber = u.Account != null ? u.Account.AccountNumber : null,
                AccountStatus = u.Account != null ? u.Account.Status : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return ApiResponse<UserApprovalListItemResponse>.ErrorResponse("User not found.");

        return ApiResponse<UserApprovalListItemResponse>.SuccessResponse(user);
    }

    public async Task<ApiResponse<object>> ApproveOrRejectUserAsync(int userId, ApproveOrRejectUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.Include(u => u.Account).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            return ApiResponse<object>.ErrorResponse("User not found.");

        user.ApprovalStatus = request.ApprovalStatus;

        if (request.ApprovalStatus == ApprovalStatus.Approved)
        {
            if (user.Account == null)
            {
                var account = new Account
                {
                    UserId = user.Id,
                    Balance = 0,
                    Currency = "USD",
                    Status = AccountStatus.Active,
                    AccountNumber = "SM" + Guid.NewGuid().ToString("N")[..16]
                };
                db.Accounts.Add(account);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                user.Account.Status = AccountStatus.Active;
            }
        }
        else if (request.ApprovalStatus == ApprovalStatus.Rejected && user.Account != null)
        {
            user.Account.Status = AccountStatus.Closed;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("User", userId.ToString(), request.ApprovalStatus == ApprovalStatus.Approved ? "UserApproved" : "UserRejected", new { request.ApprovalStatus }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.SuccessResponse(new { });
    }

    public async Task<ApiResponse<object>> SetAccountStatusAsync(int accountId, SetAccountStatusRequest request, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts.FindAsync([accountId], cancellationToken);
        if (account == null)
            return ApiResponse<object>.ErrorResponse("Account not found.");

        account.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Account", accountId.ToString(), "AccountStatusChanged", new { request.Status }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.SuccessResponse(new { });
    }

    public async Task<ApiResponse<object>> VerifyIcDocumentAsync(int documentId, VerifyKycDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var doc = await db.IcDocuments.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (doc == null)
            return ApiResponse<object>.ErrorResponse("Document not found.");

        doc.Status = request.Status;
        doc.User.KycStatus = request.Status == KycDocumentStatus.Verified ? KycStatus.Verified : request.Status == KycDocumentStatus.Rejected ? KycStatus.Rejected : KycStatus.Pending;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("IcDocument", documentId.ToString(), request.Status == KycDocumentStatus.Verified ? "KycVerified" : "KycRejected", new { request.Status }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.SuccessResponse(new { });
    }

    public async Task<ApiResponse<object>> VerifyPassportDocumentAsync(int documentId, VerifyKycDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var doc = await db.PassportDocuments.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (doc == null)
            return ApiResponse<object>.ErrorResponse("Document not found.");

        doc.Status = request.Status;
        doc.User.KycStatus = request.Status == KycDocumentStatus.Verified ? KycStatus.Verified : request.Status == KycDocumentStatus.Rejected ? KycStatus.Rejected : KycStatus.Pending;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("PassportDocument", documentId.ToString(), request.Status == KycDocumentStatus.Verified ? "KycVerified" : "KycRejected", new { request.Status }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.SuccessResponse(new { });
    }

    public async Task<ApiResponse<object>> VerifyLivingIdentityDocumentAsync(int documentId, VerifyKycDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var doc = await db.LivingIdentityDocuments.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (doc == null)
            return ApiResponse<object>.ErrorResponse("Document not found.");

        doc.Status = request.Status;
        doc.User.KycStatus = request.Status == KycDocumentStatus.Verified ? KycStatus.Verified : request.Status == KycDocumentStatus.Rejected ? KycStatus.Rejected : KycStatus.Pending;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("LivingIdentityDocument", documentId.ToString(), request.Status == KycDocumentStatus.Verified ? "KycVerified" : "KycRejected", new { request.Status }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.SuccessResponse(new { });
    }
}
