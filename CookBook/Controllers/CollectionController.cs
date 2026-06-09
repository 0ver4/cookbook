using CookBook.Models;
using CookBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Controllers;

[Authorize]
public class CollectionController : Controller
{
    private readonly ICollectionService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public CollectionController(ICollectionService service, UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    private int CurrentUserId => int.Parse(_userManager.GetUserId(User)!);

    // GET: /Collection
    public async Task<IActionResult> Index()
    {
        var collections = await _service.GetForUserAsync(CurrentUserId);
        return View(collections);
    }

    // GET: /Collection/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var col = await _service.GetDetailsAsync(id, CurrentUserId);
        if (col is null) return NotFound();
        return View(col);
    }

    // POST: /Collection/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        var (success, error, colId) = await _service.CreateAsync(CurrentUserId, name);
        if (!success)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Utworzono kolekcję.";
        return RedirectToAction(nameof(Details), new { id = colId });
    }

    // POST: /Collection/Rename
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(int id, string name)
    {
        var (success, error) = await _service.RenameAsync(id, CurrentUserId, name);
        TempData[success ? "Success" : "Error"] = success ? "Zmieniono nazwę kolekcji." : error;
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Collection/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (success, error) = await _service.DeleteAsync(id, CurrentUserId);
        TempData[success ? "Success" : "Error"] = success ? "Usunięto kolekcję." : error;
        return RedirectToAction(nameof(Index));
    }

    // POST: /Collection/AddRecipe
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRecipe(int collectionId, int recipeId, int? newCollectionFromRecipe, string? newCollectionName)
    {
        // Można też tworzyć nową kolekcję bezpośrednio ze strony przepisu
        if (newCollectionFromRecipe == 0 && !string.IsNullOrWhiteSpace(newCollectionName))
        {
            var (created, createError, newId) = await _service.CreateAsync(CurrentUserId, newCollectionName);
            if (!created)
            {
                TempData["Error"] = createError;
                return RedirectToAction("Details", "Recipe", new { id = recipeId });
            }
            collectionId = newId;
        }

        var (success, error) = await _service.AddRecipeAsync(collectionId, CurrentUserId, recipeId);
        TempData[success ? "Success" : "Error"] = success ? "Dodano przepis do kolekcji." : error;

        // Wróć do strony, z której przyszło żądanie
        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer))
            return Redirect(referer);

        return RedirectToAction(nameof(Details), new { id = collectionId });
    }

    // POST: /Collection/RemoveRecipe
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRecipe(int collectionId, int recipeId, string? returnTo)
    {
        var (success, error) = await _service.RemoveRecipeAsync(collectionId, CurrentUserId, recipeId);
        TempData[success ? "Success" : "Error"] = success ? "Usunięto przepis z kolekcji." : error;

        return returnTo == "collection"
            ? RedirectToAction(nameof(Details), new { id = collectionId })
            : RedirectToAction("Details", "Recipe", new { id = recipeId });
    }
}
