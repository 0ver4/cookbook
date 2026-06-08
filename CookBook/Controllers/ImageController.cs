using CookBook.Models;
using CookBook.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Controllers;

[AllowAnonymous]
public class ImageController : Controller
{
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
            return File(image.Data, image.ContentType ?? "application/octet-stream");

        if (!string.IsNullOrEmpty(image.Url))
            return Redirect(image.Url);

        return NotFound();
    }
}
