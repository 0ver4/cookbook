using CookBook.Dtos;
using CookBook.Models;
using CookBook.Services;
using CookBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Controllers;

public class RecipeController : Controller
{
    private readonly IRecipeService _service;
    private readonly IShoppingListService _shoppingListService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RecipeController(
        IRecipeService service,
        IShoppingListService shoppingListService,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _shoppingListService = shoppingListService;
        _userManager = userManager;
    }

    private int CurrentUserId => int.Parse(_userManager.GetUserId(User)!);
    private bool IsModerator => User.IsInRole("Moderator");

    // GET: /Recipe
    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] RecipeQuery query)
    {
        var vm = await _service.GetListAsync(query);
        return View(vm);
    }

    // GET: /Recipe/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var recipe = await _service.GetDetailsAsync(id);
        if (recipe is null)
            return NotFound();

        ViewBag.CanEdit = User.Identity?.IsAuthenticated == true
                          && (recipe.OwnerId == CurrentUserId || IsModerator);

        if (User.Identity?.IsAuthenticated == true)
        {
            var lists = await _shoppingListService.GetForUserAsync(CurrentUserId);
            ViewBag.ShoppingLists = lists.Select(l => new LookupItem(l.Id, l.Name)).ToList();
        }

        return View(recipe);
    }

    // GET: /Recipe/Create
    [Authorize]
    public async Task<IActionResult> Create()
    {
        var vm = new RecipeFormViewModel
        {
            Ingredients = { new RecipeIngredientInput() },
            Steps = { new RecipeStepInput { Content = "" } }
        };
        await _service.PopulateLookupsAsync(vm);
        return View(vm);
    }

    // POST: /Recipe/Create
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecipeFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await _service.PopulateLookupsAsync(vm);
            return View(vm);
        }

        var (success, error, recipeId) = await _service.CreateAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            await _service.PopulateLookupsAsync(vm);
            return View(vm);
        }

        TempData["Success"] = "Dodano przepis.";
        return RedirectToAction(nameof(Details), new { id = recipeId });
    }

    // GET: /Recipe/Edit/5
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _service.GetForEditAsync(id);
        if (vm is null)
            return NotFound();

        if (vm.Id != 0 && !await UserCanModify(id))
            return Forbid();

        return View(vm);
    }

    // POST: /Recipe/Edit/5
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RecipeFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await _service.PopulateLookupsAsync(vm);
            return View(vm);
        }

        var (success, error) = await _service.UpdateAsync(vm, CurrentUserId, IsModerator);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            await _service.PopulateLookupsAsync(vm);
            return View(vm);
        }

        TempData["Success"] = "Zapisano zmiany.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    // GET: /Recipe/Delete/5
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var recipe = await _service.GetDetailsAsync(id);
        if (recipe is null)
            return NotFound();

        if (!(recipe.OwnerId == CurrentUserId || IsModerator))
            return Forbid();

        return View(recipe);
    }

    // POST: /Recipe/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (success, error) = await _service.DeleteAsync(id, CurrentUserId, IsModerator);
        if (!success)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Usunięto przepis.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int recipeId, string content, int? replyToId)
    {
        var result = await _service.AddCommentAsync(recipeId, CurrentUserId, content, replyToId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Details), new { id = recipeId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int recipeId, int commentId)
    {
        var result = await _service.DeleteCommentAsync(commentId, CurrentUserId, IsModerator);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Details), new { id = recipeId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCommentReaction(int recipeId, int commentId, int reactionId)
    {
        var result = await _service.ToggleCommentReactionAsync(commentId, CurrentUserId, reactionId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Details), new { id = recipeId });
    }

    private async Task<bool> UserCanModify(int recipeId)
    {
        var details = await _service.GetDetailsAsync(recipeId);
        return details is not null && (details.OwnerId == CurrentUserId || IsModerator);
    }
}
