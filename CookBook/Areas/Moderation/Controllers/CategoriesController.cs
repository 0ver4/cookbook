using CookBook.Dtos;
using CookBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Areas.Moderation.Controllers;

// Słowniki (kategorie, tagi itd.) edytuje moderator.
[Area("Moderation")]
[Authorize(Roles = "Moderator")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    // GET: /Moderation/Categories
    public async Task<IActionResult> Index()
    {
        var categories = await _service.GetAllAsync();
        return View(categories);
    }

    // GET: /Moderation/Categories/Create
    public IActionResult Create() => View(new CreateCategoryDto());

    // POST: /Moderation/Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var (success, error) = await _service.CreateAsync(dto);
        if (!success)
        {
            ModelState.AddModelError(nameof(dto.Name), error!);
            return View(dto);
        }

        TempData["Success"] = "Dodano kategorię.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Moderation/Categories/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _service.GetByIdAsync(id);
        if (category is null)
            return NotFound();

        return View(new UpdateCategoryDto { Id = category.Id, Name = category.Name });
    }

    // POST: /Moderation/Categories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var (success, error) = await _service.UpdateAsync(dto);
        if (!success)
        {
            ModelState.AddModelError(nameof(dto.Name), error!);
            return View(dto);
        }

        TempData["Success"] = "Zapisano zmiany.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Moderation/Categories/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _service.GetByIdAsync(id);
        if (category is null)
            return NotFound();

        return View(category);
    }

    // POST: /Moderation/Categories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _service.DeleteAsync(id);
        TempData["Success"] = "Usunięto kategorię.";
        return RedirectToAction(nameof(Index));
    }
}
