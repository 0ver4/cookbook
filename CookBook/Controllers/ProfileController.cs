using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CookBook.Models;
using CookBook.Data;
using CookBook.ViewModels;

namespace CookBook.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CookBookContext _context;

    public ProfileController(UserManager<ApplicationUser> userManager, CookBookContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Public(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var recipes = await _context.Recipes
            .Include(r => r.DifficultyLevel)
            .Include(r => r.Images)
            .Where(r => r.UserId == id && r.IsPublished && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var reviews = await _context.Reviews
            .Include(r => r.Recipe)
            .Where(r => r.UserId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var vm = new PublicProfileViewModel(user, recipes, reviews);
        return View(vm);
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Nie znaleziono użytkownika.");
        }

        // Pobieramy rolę
        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.UserRole = roles.FirstOrDefault() ?? "Brak roli";

        // Liczniki z widoku vw_UserStats (agregacja po stronie bazy, jeden wiersz na użytkownika)
        var stats = await _context.UserStats.FirstAsync(s => s.UserId == user.Id);
        ViewBag.RecipesCount = stats.RecipeCount;
        ViewBag.CommentsCount = stats.CommentCount;
        ViewBag.FavoritesCount = stats.CollectionCount;
        ViewBag.UserStats = stats;

        var lastActivities = await _context.Recipes
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(3)
            .ToListAsync();
        ViewBag.LastActivities = lastActivities;

        user.Collections = await _context.Collections
            .Include(c => c.Recipes)
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Nie znaleziono użytkownika.");
        }
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string firstName, string lastName)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Nie znaleziono użytkownika.");
        }

        user.FirstName = firstName;
        user.LastName = lastName;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(user);
    }
}