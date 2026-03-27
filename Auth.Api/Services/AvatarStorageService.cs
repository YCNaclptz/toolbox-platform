using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Auth.Api.Services;

public class AvatarStorageService
{
    private readonly BlobContainerClient _container;
    private const long MaxFileSize = 2 * 1024 * 1024; // 2MB
    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public AvatarStorageService(BlobServiceClient blobServiceClient, IConfiguration config)
    {
        var containerName = config["AzureBlobStorage:ContainerName"] ?? "avatars";
        _container = blobServiceClient.GetBlobContainerClient(containerName);
        _container.CreateIfNotExists();
    }

    public async Task<string> UploadAvatarAsync(int userId, Stream imageStream, string contentType)
    {
        if (imageStream.Length > MaxFileSize)
            throw new InvalidOperationException("檔案大小不能超過 2MB");

        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException("僅支援 JPEG、PNG、WebP 格式");

        var extension = contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpg"
        };

        var blobName = $"{userId}/{Guid.NewGuid()}.{extension}";
        var blobClient = _container.GetBlobClient(blobName);

        await blobClient.UploadAsync(imageStream, new BlobHttpHeaders { ContentType = contentType });

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAvatarAsync(string? avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl)) return;

        try
        {
            var uri = new Uri(avatarUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 3)
            {
                var blobName = string.Join("/", segments.Skip(1));
                var blobClient = _container.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
            }
        }
        catch
        {
            // Silently ignore deletion errors for old avatars
        }
    }
}