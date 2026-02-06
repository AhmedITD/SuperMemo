using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Kyc;

public class KycStatusResponse
{
    public KycStatus KycStatus { get; set; }
    public KybStatus KybStatus { get; set; }
    /// <summary>Which document type was submitted (Ic, Passport, LivingIdentity) or null if none.</summary>
    public string? DocumentType { get; set; }
    public KycDocumentStatus? DocumentStatus { get; set; }
}
