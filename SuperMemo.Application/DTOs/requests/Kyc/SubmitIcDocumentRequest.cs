namespace SuperMemo.Application.DTOs.requests.Kyc;

public class SubmitIcDocumentRequest
{
    public required string IdentityCardNumber { get; set; }
    public required string FullName { get; set; }
    public required string MotherFullName { get; set; }
    public DateTime BirthDate { get; set; }
    public required string BirthLocation { get; set; }
}
