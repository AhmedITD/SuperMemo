namespace SuperMemo.Application.DTOs.requests.Auth;

public class RegisterLivingIdentityDocumentDto
{
    public required string SerialNumber { get; set; }
    public required string FullFamilyName { get; set; }
    public required string LivingLocation { get; set; }
    public required string FormNumber { get; set; }
    public string? ImageUrl { get; set; }
}
