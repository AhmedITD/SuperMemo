using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.DTOs.requests.Kyc;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Kyc;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Kyc;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class KycService(ISuperMemoDbContext db) : IKycService
{
    public async Task<ApiResponse<int>> SubmitIcDocumentAsync(SubmitIcDocumentRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var doc = new IcDocument
        {
            UserId = userId,
            IdentityCardNumber = request.IdentityCardNumber,
            FullName = request.FullName,
            MotherFullName = request.MotherFullName,
            BirthDate = request.BirthDate,
            BirthLocation = request.BirthLocation,
            Status = KycDocumentStatus.Pending
        };
        db.IcDocuments.Add(doc);
        await db.SaveChangesAsync(cancellationToken);

        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user != null)
        {
            user.KycStatus = KycStatus.Pending;
            await db.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<int>.SuccessResponse(doc.Id);
    }

    public async Task<ApiResponse<int>> SubmitPassportDocumentAsync(SubmitPassportDocumentRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var doc = new PassportDocument
        {
            UserId = userId,
            PassportNumber = request.PassportNumber,
            FullName = request.FullName,
            ShortName = request.ShortName,
            Nationality = request.Nationality,
            BirthDate = request.BirthDate,
            MotherFullName = request.MotherFullName,
            ExpiryDate = request.ExpiryDate,
            Status = KycDocumentStatus.Pending
        };
        db.PassportDocuments.Add(doc);
        await db.SaveChangesAsync(cancellationToken);

        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user != null)
        {
            user.KycStatus = KycStatus.Pending;
            await db.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<int>.SuccessResponse(doc.Id);
    }

    public async Task<ApiResponse<int>> SubmitLivingIdentityDocumentAsync(SubmitLivingIdentityDocumentRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var doc = new LivingIdentityDocument
        {
            UserId = userId,
            SerialNumber = request.SerialNumber,
            FullFamilyName = request.FullFamilyName,
            LivingLocation = request.LivingLocation,
            FormNumber = request.FormNumber,
            Status = KycDocumentStatus.Pending
        };
        db.LivingIdentityDocuments.Add(doc);
        await db.SaveChangesAsync(cancellationToken);

        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user != null)
        {
            user.KycStatus = KycStatus.Pending;
            await db.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<int>.SuccessResponse(doc.Id);
    }

    public async Task<ApiResponse<KycStatusResponse>> GetStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            return ApiResponse<KycStatusResponse>.ErrorResponse("User not found.", code: "RESOURCE_NOT_FOUND");

        string? documentType = null;
        KycDocumentStatus? documentStatus = null;

        var ic = await db.IcDocuments.Where(d => d.UserId == userId).OrderByDescending(d => d.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (ic != null) { documentType = "Ic"; documentStatus = ic.Status; }
        else
        {
            var pass = await db.PassportDocuments.Where(d => d.UserId == userId).OrderByDescending(d => d.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            if (pass != null) { documentType = "Passport"; documentStatus = pass.Status; }
            else
            {
                var liv = await db.LivingIdentityDocuments.Where(d => d.UserId == userId).OrderByDescending(d => d.CreatedAt).FirstOrDefaultAsync(cancellationToken);
                if (liv != null) { documentType = "LivingIdentity"; documentStatus = liv.Status; }
            }
        }

        var response = new KycStatusResponse
        {
            KycStatus = user.KycStatus,
            KybStatus = user.KybStatus,
            DocumentType = documentType,
            DocumentStatus = documentStatus
        };
        return ApiResponse<KycStatusResponse>.SuccessResponse(response);
    }
}
