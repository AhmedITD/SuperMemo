using Microsoft.Extensions.Options;
using SuperMemo.Application.Interfaces.Storage;
using SuperMemo.Infrastructure.Options;

namespace SuperMemo.Infrastructure.Services.Storage;

public class FileStorageService : IStorageService
{
    private readonly StorageOptions _options;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

    public FileStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveKycImageAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            ext = GetExtensionFromContentType(contentType);

        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_options.BasePath, uniqueName);

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using (var fileStream = File.Create(fullPath))
            await content.CopyToAsync(fileStream, cancellationToken);

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/{uniqueName}";
    }

    public async Task<string> SaveUserImageAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            ext = GetExtensionFromContentType(contentType);

        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_options.UserImagesPath, uniqueName);

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using (var fileStream = File.Create(fullPath))
            await content.CopyToAsync(fileStream, cancellationToken);

        var baseUrl = _options.UserImagesBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{uniqueName}";
    }

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
    }
}
