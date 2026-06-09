using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CookBook.Models;
using CookBook.Data;

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

        //  pobieranie liczników z bazy danych
        ViewBag.RecipesCount = await _context.Recipes.CountAsync(r => r.UserId == user.Id);
        ViewBag.CommentsCount = await _context.Comments.CountAsync(c => c.UserId == user.Id);
        
        ViewBag.FavoritesCount = 0; 

        var lastActivities = await _context.Recipes
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.Id) // lub po dacie, np. r.CreatedAt jeśli istnieje
            .Take(3)
            .ToListAsync();

        ViewBag.LastActivities = lastActivities;

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