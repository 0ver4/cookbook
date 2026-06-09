using CookBook.Models;
using CookBook.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Controllers;

[AllowAnonymous]
public class ImageController : Controller
{
    // Tylko te typy serwujemy z zadeklarowanym Content-Type; cokolwiek innego
    // (np. text/html podstawione przez atakującego) serwujemy jako pobranie binarne.
    private static readonly HashSet<string> AllowedImageTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    private readonly IRepository<Image> _images;

    public ImageController(IRepository<Image> images)
    {
        _images = images;
    }

    // GET: /Image/5  -> zwraca zawartość pliku z bazy
    [HttpGet("Image/{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var image = await _images.GetByIdAsync(id);
        if (image is null)
            return NotFound();

        // Wgrany plik (blob) serwujemy wprost; obrazek zewnętrzny - przekierowaniem
        if (image.Data is { Length: > 0 })
        {
            // Nie ufamy Content-Type podanemu przez klienta przy uploadzie: wymuszamy
            // bezpieczny typ z allowlisty + nosniff, żeby przeglądarka nie wyrenderowała
            // podstawionego HTML/JS jako strony (stored XSS).
            var safeType = AllowedImageTypes.Contains(image.ContentType ?? "")
                ? image.ContentType!
                : "application/octet-stream";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(image.Data, safeType);
        }

        if (!string.IsNullOrEmpty(image.Url))
            return Redirect(image.Url);

        return NotFound();
    }
}
