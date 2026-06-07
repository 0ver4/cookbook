using CookBook.Dtos;
using CookBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Controllers;

// Słowniki (kategorie, tagi itd.) edytuje moderator.
[Authorize(Roles = "Moderator")]
public class CategoryController : Controller
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service)
    {
        _service = service;
    }

    // GET: /Category
    public async Task<IActionResult> Index()
    {
        var categories = await _service.GetAllAsync();
        return View(categories);
    }

    // GET: /Category/Create
    public IActionResult Create() => View(new CreateCategoryDto());

    // POST: /Category/Create
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

    // GET: /Category/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _service.GetByIdAsync(id);
        if (category is null)
            return NotFound();

        return View(new UpdateCategoryDto { Id = category.Id, Name = category.Name });
    }

    // POST: /Category/Edit/5
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

    // GET: /Category/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _service.GetByIdAsync(id);
        if (category is null)
            return NotFound();

        return View(category);
    }

    // POST: /Category/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _service.DeleteAsync(id);
        TempData["Success"] = "Usunięto kategorię.";
        return RedirectToAction(nameof(Index));
    }
}
