using Microsoft.AspNetCore.Http;

namespace CookBook.Services;

public interface IImageService
{
    /// <summary>
    /// Zapisuje przesłany plik na dysku lokalnym (wwwroot/uploads/recipes).
    /// Zwraca względny URL albo komunikat błędu walidacji.
    /// </summary>
    Task<(string? Url, string? Error)> SaveAsync(IFormFile file);

    /// <summary>Usuwa plik z dysku na podstawie względnego URL-a (best-effort).</summary>
    void Delete(string url);
}
