using Microsoft.AspNetCore.Http;

namespace CookBook.Services;

public class ImageService : IImageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<(byte[]? Data, string? ContentType, string? Error)> ReadAsync(IFormFile file)
    {
        if (file.Length == 0)
            return (null, null, "Plik jest pusty.");

        if (file.Length > MaxBytes)
            return (null, null, "Plik jest za duży (max 5 MB).");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return (null, null, "Dozwolone formaty: jpg, jpeg, png, webp.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;
        return (stream.ToArray(), contentType, null);
    }
}
