using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.requests.Admin;

public class VerifyKycDocumentRequest
{
    public KycDocumentStatus Status { get; set; }
}
