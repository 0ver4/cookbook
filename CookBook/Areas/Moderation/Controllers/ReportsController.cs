using CookBook.Models;
using CookBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Areas.Moderation.Controllers;

[Area("Moderation")]
[Authorize(Roles = "Moderator")]
public class ReportsController : Controller
{
    private readonly IReportService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(IReportService service, UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    private int CurrentUserId => int.Parse(_userManager.GetUserId(User)!);

    public async Task<IActionResult> Index(bool showResolved = false)
    {
        var vm = await _service.GetReportsAsync(showResolved);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveRecipe(int id, bool showResolved = false)
    {
        var (success, error) = await _service.SetRecipeReportStatusAsync(id, CurrentUserId, ReportStatus.Resolved);
        SetFlash(success, error, "Oznaczono zgłoszenie jako rozpatrzone.");
        return RedirectToAction(nameof(Index), new { showResolved });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissRecipe(int id, bool showResolved = false)
    {
        var (success, error) = await _service.SetRecipeReportStatusAsync(id, CurrentUserId, ReportStatus.Dismissed);
        SetFlash(success, error, "Odrzucono zgłoszenie.");
        return RedirectToAction(nameof(Index), new { showResolved });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRecipe(int id, bool showResolved = false)
    {
        var (success, error) = await _service.DeleteReportedRecipeAsync(id, CurrentUserId);
        SetFlash(success, error, "Usunięto zgłoszony przepis.");
        return RedirectToAction(nameof(Index), new { showResolved });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveComment(int id, bool showResolved = false)
    {
        var (success, error) = await _service.SetCommentReportStatusAsync(id, CurrentUserId, ReportStatus.Resolved);
        SetFlash(success, error, "Oznaczono zgłoszenie jako rozpatrzone.");
        return RedirectToAction(nameof(Index), new { showResolved });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissComment(int id, bool showResolved = false)
    {
        var (success, error) = await _service.SetCommentReportStatusAsync(id, CurrentUserId, ReportStatus.Dismissed);
        SetFlash(success, error, "Odrzucono zgłoszenie.");
        return RedirectToAction(nameof(Index), new { showResolved });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, bool showResolved = false)
    {
        var (success, error) = await _service.DeleteReportedCommentAsync(id, CurrentUserId);
        SetFlash(success, error, "Usunięto zgłoszony komentarz.");
        return RedirectToAction(nameof(Index), new { showResolved });
    }

    private void SetFlash(bool success, string? error, string successMessage)
    {
        if (success)
            TempData["Success"] = successMessage;
        else
            TempData["Error"] = error;
    }
}
