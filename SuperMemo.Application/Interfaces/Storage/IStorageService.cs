namespace SuperMemo.Application.Interfaces.Storage;

/// <summary>
/// Saves KYC document images to external storage and returns the public URL.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads an image and returns its public URL.
    /// </summary>
    /// <param name="content">Image stream.</param>
    /// <param name="fileName">Suggested file name (extension used for content type if needed).</param>
    /// <param name="contentType">MIME type (e.g. image/jpeg).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL to access the stored image.</returns>
    Task<string> SaveKycImageAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a user profile/avatar image and returns its public URL.
    /// </summary>
    Task<string> SaveUserImageAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}
