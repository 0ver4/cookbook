using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace CookBook.Services;

public class ImageService : IImageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    private const string RelativeFolder = "uploads/recipes";

    private readonly IWebHostEnvironment _env;

    public ImageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<(string? Url, string? Error)> SaveAsync(IFormFile file)
    {
        if (file.Length == 0)
            return (null, "Plik jest pusty.");

        if (file.Length > MaxBytes)
            return (null, "Plik jest za duży (max 5 MB).");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return (null, "Dozwolone formaty: jpg, jpeg, png, webp.");

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var targetDir = Path.Combine(webRoot, RelativeFolder);
        Directory.CreateDirectory(targetDir);

        // Unikalna nazwa pliku - chroni przed kolizjami i unique constraintem na Image.Url
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetDir, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return ($"/{RelativeFolder}/{fileName}", null);
    }

    public void Delete(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var fullPath = Path.Combine(webRoot, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
