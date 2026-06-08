using CookBook.Models;
using CookBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Controllers;

[Authorize] // plan posiłków jest osobisty
public class MealPlanController : Controller
{
    private readonly IMealPlanService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public MealPlanController(IMealPlanService service, UserManager<ApplicationUser> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    private int CurrentUserId => int.Parse(_userManager.GetUserId(User)!);

    // GET: /MealPlan?weekStart=2026-06-08
    public async Task<IActionResult> Index(DateTime? weekStart)
    {
        var anchor = weekStart ?? DateTime.Today;
        var vm = await _service.GetWeekAsync(CurrentUserId, anchor);
        return View(vm);
    }

    // POST: /MealPlan/AddItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(DateTime date, MealType mealType, int recipeId)
    {
        var (success, error) = await _service.AddAsync(CurrentUserId, date, mealType, recipeId);
        TempData[success ? "Success" : "Error"] = success ? "Dodano do planu." : error;
        return RedirectToAction(nameof(Index), new { weekStart = date.ToString("yyyy-MM-dd") });
    }

    // POST: /MealPlan/RemoveItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int id, DateTime weekStart)
    {
        var (success, error) = await _service.RemoveAsync(CurrentUserId, id);
        if (!success)
            TempData["Error"] = error;

        return RedirectToAction(nameof(Index), new { weekStart = weekStart.ToString("yyyy-MM-dd") });
    }
}
