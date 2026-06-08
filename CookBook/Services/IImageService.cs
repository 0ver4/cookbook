using Microsoft.AspNetCore.Http;

namespace CookBook.Services;

public interface IImageService
{
    /// <summary>
    /// Waliduje i wczytuje przesłany plik do pamięci (do zapisania jako blob w bazie).
    /// Zwraca zawartość i typ MIME albo komunikat błędu walidacji.
    /// </summary>
    Task<(byte[]? Data, string? ContentType, string? Error)> ReadAsync(IFormFile file);
}
