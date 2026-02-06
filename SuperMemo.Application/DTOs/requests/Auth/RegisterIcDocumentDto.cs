namespace SuperMemo.Application.DTOs.requests.Auth;

public class RegisterIcDocumentDto
{
    public required string IdentityCardNumber { get; set; }
    public required string FullName { get; set; }
    public required string MotherFullName { get; set; }
    public DateTime BirthDate { get; set; }
    public required string BirthLocation { get; set; }
    public string? ImageUrl { get; set; }
}
