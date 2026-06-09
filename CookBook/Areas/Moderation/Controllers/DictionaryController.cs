using CookBook.Dtos;
using CookBook.Models;
using CookBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Areas.Moderation.Controllers;

// Jeden kontroler dla wszystkich prostych słowników { Id, Name }.
// Konkretny słownik wybierany jest przez slug w trasie (np. /Moderation/Dictionary/tags).
[Area("Moderation")]
[Authorize(Roles = "Moderator")]
[Route("Moderation/Dictionary")]
public class DictionaryController : Controller
{
    // Rozwiązuje LookupService<TEncja> z kontenera na podstawie typu z rejestru.
    private ILookupOps Ops(Type entityType) =>
        (ILookupOps)HttpContext.RequestServices
            .GetRequiredService(typeof(LookupService<>).MakeGenericType(entityType));

    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string slug)
    {
        if (LookupRegistry.Find(slug) is not { } d) return NotFound();
        var items = await Ops(d.EntityType).GetAllAsync();
        return View(new LookupListVm(d, items));
    }

    [HttpGet("{slug}/Create")]
    public IActionResult Create(string slug) =>
        LookupRegistry.Find(slug) is { } d ? View("Form", new LookupFormVm(d, 0, "")) : NotFound();

    [HttpPost("{slug}/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string slug, string name)
    {
        if (LookupRegistry.Find(slug) is not { } d) return NotFound();

        var (success, error) = await Ops(d.EntityType).CreateAsync(name);
        if (!success)
        {
            ModelState.AddModelError("Name", error!);
            return View("Form", new LookupFormVm(d, 0, name));
        }

        TempData["Success"] = $"Dodano: {d.Singular}.";
        return RedirectToAction(nameof(Index), new { slug });
    }

    [HttpGet("{slug}/Edit/{id:int}")]
    public async Task<IActionResult> Edit(string slug, int id)
    {
        if (LookupRegistry.Find(slug) is not { } d) return NotFound();

        var item = await Ops(d.EntityType).GetByIdAsync(id);
        if (item is null) return NotFound();

        return View("Form", new LookupFormVm(d, item.Value.Id, item.Value.Name));
    }

    [HttpPost("{slug}/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string slug, int id, string name)
    {
        if (LookupRegistry.Find(slug) is not { } d) return NotFound();

        var (success, error) = await Ops(d.EntityType).UpdateAsync(id, name);
        if (!success)
        {
            ModelState.AddModelError("Name", error!);
            return View("Form", new LookupFormVm(d, id, name));
        }

        TempData["Success"] = "Zapisano zmiany.";
        return RedirectToAction(nameof(Index), new { slug });
    }

    [HttpGet("{slug}/Delete/{id:int}")]
    public async Task<IActionResult> Delete(string slug, int id)
    {
        if (LookupRegistry.Find(slug) is not { } d) return NotFound();

        var item = await Ops(d.EntityType).GetByIdAsync(id);
        if (item is null) return NotFound();

        return View(new LookupFormVm(d, item.Value.Id, item.Value.Name));
    }

    [HttpPost("{slug}/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string slug, int id)
    {
        if (LookupRegistry.Find(slug) is not { } d) return NotFound();

        await Ops(d.EntityType).DeleteAsync(id);
        TempData["Success"] = "Usunięto pozycję.";
        return RedirectToAction(nameof(Index), new { slug });
    }
}
