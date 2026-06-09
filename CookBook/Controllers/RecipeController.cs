using CookBook.Dtos;
using CookBook.Models;
using CookBook.Services;
using CookBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace CookBook.Controllers;

public class RecipeController : Controller
{
    private readonly IRecipeService _service;
    private readonly IShoppingListService _shoppingListService;
    private readonly ICollectionService _collectionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RecipeController(
        IRecipeService service,
        IShoppingListService shoppingListService,
        ICollectionService collectionService,
        UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _shoppingListService = shoppingListService;
        _collectionService = collectionService;
        _userManager = userManager;
    }

    private int CurrentUserId => int.Parse(_userManager.GetUserId(User)!);
    private bool IsModerator => User.IsInRole("Moderator");

    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] RecipeQuery query)
    {
        var vm = await _service.GetListAsync(query);
        return View(vm);
    }

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

            var collections = await _collectionService.GetForUserAsync(CurrentUserId);
            ViewBag.Collections = collections.Select(c => new LookupItem(c.Id, c.Name)).ToList();
        }

        return View(recipe);
    }

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
    public async Task<IActionResult> AddReview(int recipeId, int rating)
    {
        var result = await _service.AddReviewAsync(recipeId, CurrentUserId, rating);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Details), new { id = recipeId });
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

    [AllowAnonymous]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var recipe = await _service.GetDetailsAsync(id);
        if (recipe is null)
            return NotFound();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40); 
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));

                page.Header().Text(recipe.Name)
                    .SemiBold().FontSize(24).FontColor(Colors.Indigo.Medium);

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(15);

                    column.Item().Text($"Autor: {recipe.AuthorName}  |  Trudność: {recipe.DifficultyName}  |  Data: {recipe.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy")}")
                        .Italic().FontColor(Colors.Grey.Medium);

                    if (!string.IsNullOrWhiteSpace(recipe.Description))
                    {
                        column.Item().Text(recipe.Description).LineHeight(1.3f);
                    }

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Text("Składniki:").Bold().FontSize(14).FontColor(Colors.Indigo.Darken2);
                    foreach (var ing in recipe.Ingredients)
                    {
                        column.Item().Text($"• {ing.IngredientName} - {ing.Amount.ToString("0.##")} {ing.UnitName}");
                    }

                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Text("Sposób przygotowania:").Bold().FontSize(14).FontColor(Colors.Indigo.Darken2);
                    foreach (var step in recipe.Steps.OrderBy(s => s.Order))
                    {
                        column.Item().Text($"{step.Order}. {step.Content}").LineHeight(1.2f);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Strona ");
                    x.CurrentPageNumber();
                });
            });
        });

        var pdfBytes = document.GeneratePdf();
        string safeFileName = recipe.Name.Replace(" ", "_") + "_przepis.pdf";
        
        return File(pdfBytes, "application/pdf", safeFileName);
    }

    private async Task<bool> UserCanModify(int recipeId)
    {
        var details = await _service.GetDetailsAsync(recipeId);
        return details is not null && (details.OwnerId == CurrentUserId || IsModerator);
    }
}