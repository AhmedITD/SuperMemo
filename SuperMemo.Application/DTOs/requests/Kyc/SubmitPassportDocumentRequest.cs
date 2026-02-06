namespace SuperMemo.Application.DTOs.requests.Kyc;

public class SubmitPassportDocumentRequest
{
    public required string PassportNumber { get; set; }
    public required string FullName { get; set; }
    public string? ShortName { get; set; }
    public required string Nationality { get; set; }
    public DateTime BirthDate { get; set; }
    public required string MotherFullName { get; set; }
    public DateTime ExpiryDate { get; set; }
}
