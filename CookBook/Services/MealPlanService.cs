using CookBook.Models;
using CookBook.Repositories;
using CookBook.ViewModels;

namespace CookBook.Services;

public class MealPlanService : IMealPlanService
{
    private readonly IMealPlanRepository _mealPlan;
    private readonly IRecipeService _recipeService;

    public MealPlanService(IMealPlanRepository mealPlan, IRecipeService recipeService)
    {
        _mealPlan = mealPlan;
        _recipeService = recipeService;
    }

    public async Task<MealPlanWeekViewModel> GetWeekAsync(int userId, DateTime anyDayInWeek)
    {
        var weekStart = StartOfWeek(anyDayInWeek);
        var weekEnd = weekStart.AddDays(7);

        var items = await _mealPlan.GetForUserInRangeAsync(userId, weekStart, weekEnd);
        var recipes = await _recipeService.GetListAsync();

        var vm = new MealPlanWeekViewModel
        {
            WeekStart = weekStart,
            Recipes = recipes.Recipes.Select(r => new LookupItem(r.Id, r.Name)).ToList()
        };

        for (var i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            var day = new MealPlanDay { Date = date };

            foreach (var slot in MealTypeInfo.Ordered)
            {
                day.Meals[slot] = items
                    .Where(m => m.Date.Date == date && m.MealType == slot)
                    .Select(m => new MealPlanEntry(m.Id, m.RecipeId, m.Recipe.Name))
                    .ToList();
            }

            vm.Days.Add(day);
        }

        return vm;
    }

    public async Task<(bool Success, string? Error)> AddAsync(int userId, DateTime date, MealType mealType, int recipeId)
    {
        var recipe = await _recipeService.GetDetailsAsync(recipeId);
        if (recipe is null)
            return (false, "Nie znaleziono przepisu.");

        await _mealPlan.AddAsync(new MealPlanItem
        {
            UserId = userId,
            RecipeId = recipeId,
            MealType = mealType,
            Date = date.Date
        });
        await _mealPlan.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveAsync(int userId, int itemId)
    {
        var item = await _mealPlan.GetByIdAsync(itemId);
        if (item is null)
            return (false, "Nie znaleziono pozycji planu.");

        if (item.UserId != userId)
            return (false, "Brak uprawnień.");

        _mealPlan.Remove(item);
        await _mealPlan.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Poniedziałek tygodnia zawierającego podaną datę.</summary>
    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7; // poniedziałek = 0
        return date.Date.AddDays(-diff);
    }
}
