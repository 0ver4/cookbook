using CookBook.Dtos;
using CookBook.Models;
using CookBook.Repositories;
using CookBook.Services;
using CookBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Controllers;

[Authorize] // listy zakupów są osobiste
public class ShoppingListController : Controller
{
    private readonly IShoppingListService _service;
    private readonly IShoppingListPdfService _pdfService;
    private readonly IRecipeService _recipeService;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<Ingredient> _ingredients;
    private readonly UserManager<ApplicationUser> _userManager;

    public ShoppingListController(
        IShoppingListService service,
        IShoppingListPdfService pdfService,
        IRecipeService recipeService,
        IRepository<Unit> units,
        IRepository<Ingredient> ingredients,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _pdfService = pdfService;
        _recipeService = recipeService;
        _units = units;
        _ingredients = ingredients;
        _userManager = userManager;
    }

    private int CurrentUserId => int.Parse(_userManager.GetUserId(User)!);

    // GET: /ShoppingList
    public async Task<IActionResult> Index()
    {
        var lists = await _service.GetForUserAsync(CurrentUserId);
        return View(lists);
    }

    // GET: /ShoppingList/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var list = await _service.GetDetailsAsync(id, CurrentUserId);
        if (list is null)
            return NotFound();

        // Pełna lista przepisów do wyboru (bez paginacji - to słownik wyboru "dodaj z przepisu").
        var recipes = await _recipeService.GetListAsync(new RecipeQuery(PageSize: int.MaxValue));
        ViewBag.Recipes = recipes.Recipes.Select(r => new LookupItem(r.Id, r.Name)).ToList();

        var units = await _units.GetAllAsync();
        ViewBag.Units = units.OrderBy(u => u.Id).Select(u => new LookupItem(u.Id, u.Name)).ToList();

        var ingredients = await _ingredients.GetAllAsync();
        ViewBag.Ingredients = ingredients.OrderBy(i => i.Name).Select(i => i.Name).ToList();
        return View(list);
    }

    // POST: /ShoppingList/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        var (success, error, listId) = await _service.CreateAsync(CurrentUserId, name);
        if (!success)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Utworzono listę.";
        return RedirectToAction(nameof(Details), new { id = listId });
    }

    // POST: /ShoppingList/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (success, error) = await _service.DeleteAsync(id, CurrentUserId);
        if (!success)
            TempData["Error"] = error;
        else
            TempData["Success"] = "Usunięto listę.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /ShoppingList/AddItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int listId, string ingredientName, double amount, int? unitId)
    {
        var (success, error) = await _service.AddItemAsync(listId, CurrentUserId, ingredientName, amount, unitId);
        if (!success)
            TempData["Error"] = error;

        return RedirectToAction(nameof(Details), new { id = listId });
    }

    // POST: /ShoppingList/RemoveItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int listId, int ingredientId, int unitId)
    {
        await _service.RemoveItemAsync(listId, CurrentUserId, ingredientId, unitId);
        return RedirectToAction(nameof(Details), new { id = listId });
    }

    // POST: /ShoppingList/Toggle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int listId, int ingredientId, int unitId)
    {
        await _service.ToggleItemAsync(listId, CurrentUserId, ingredientId, unitId);
        return RedirectToAction(nameof(Details), new { id = listId });
    }

    // POST: /ShoppingList/GenerateFromRecipe
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateFromRecipe(int listId, int recipeId)
    {
        var (success, error) = await _service.GenerateFromRecipeAsync(listId, CurrentUserId, recipeId);
        TempData[success ? "Success" : "Error"] = success ? "Dodano składniki z przepisu." : error;
        return RedirectToAction(nameof(Details), new { id = listId });
    }

    // POST: /ShoppingList/AddRecipe  (wywoływane z widoku przepisu)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRecipe(int recipeId, int? listId, string? newListName)
    {
        int targetListId;
        if (listId is > 0)
        {
            targetListId = listId.Value;
        }
        else
        {
            var name = string.IsNullOrWhiteSpace(newListName) ? "Lista zakupów" : newListName;
            var (created, createError, newId) = await _service.CreateAsync(CurrentUserId, name);
            if (!created)
            {
                TempData["Error"] = createError;
                return RedirectToAction("Details", "Recipe", new { id = recipeId });
            }
            targetListId = newId;
        }

        var (success, error) = await _service.GenerateFromRecipeAsync(targetListId, CurrentUserId, recipeId);
        TempData[success ? "Success" : "Error"] = success ? "Dodano składniki do listy." : error;
        return RedirectToAction(nameof(Details), new { id = targetListId });
    }

    // GET: /ShoppingList/DownloadPdf/5
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var list = await _service.GetDetailsAsync(id, CurrentUserId);
        if (list is null)
            return NotFound();

        var bytes = _pdfService.Generate(list);
        return File(bytes, "application/pdf", $"lista-zakupow-{id}.pdf");
    }
}
