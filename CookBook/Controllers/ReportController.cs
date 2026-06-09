using CookBook.Models;
using CookBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Controllers;

// Zgłaszanie treści przez zwykłych użytkowników. Zarządzanie zgłoszeniami
// odbywa się w panelu moderacji (Areas/Moderation, ReportsController).
public class ReportController : Controller
{
    private readonly IReportService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportController(IReportService service, UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    private int CurrentUserId => int.Parse(_userManager.GetUserId(User)!);

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportRecipe(int recipeId, string reason)
    {
        var (success, error) = await _service.ReportRecipeAsync(recipeId, CurrentUserId, reason);
        SetFlash(success, error);
        return RedirectToAction("Details", "Recipe", new { id = recipeId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportComment(int commentId, int recipeId, string reason)
    {
        var (success, error) = await _service.ReportCommentAsync(commentId, CurrentUserId, reason);
        SetFlash(success, error);
        return RedirectToAction("Details", "Recipe", new { id = recipeId });
    }

    private void SetFlash(bool success, string? error)
    {
        if (success)
            TempData["Success"] = "Dziękujemy, zgłoszenie zostało wysłane.";
        else
            TempData["Error"] = error;
    }
}
