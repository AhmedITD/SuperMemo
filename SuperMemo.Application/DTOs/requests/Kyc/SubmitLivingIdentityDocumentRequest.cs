namespace SuperMemo.Application.DTOs.requests.Kyc;

public class SubmitLivingIdentityDocumentRequest
{
    public required string SerialNumber { get; set; }
    public required string FullFamilyName { get; set; }
    public required string LivingLocation { get; set; }
    public required string FormNumber { get; set; }
}
