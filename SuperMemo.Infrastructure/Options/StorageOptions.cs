namespace SuperMemo.Infrastructure.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Local or network path where KYC images are saved (e.g. C:\uploads\kyc or /var/uploads/kyc).
    /// </summary>
    public string BasePath { get; set; } = "uploads/kyc";

    /// <summary>
    /// Base URL used to build public image URLs (e.g. https://api.example.com/uploads or /uploads).
    /// </summary>
    public string BaseUrl { get; set; } = "/uploads/kyc";

    /// <summary>Path where user profile images are saved (e.g. uploads/users).</summary>
    public string UserImagesPath { get; set; } = "uploads/users";

    /// <summary>Base URL for user profile images (e.g. /uploads/users).</summary>
    public string UserImagesBaseUrl { get; set; } = "/uploads/users";
}
