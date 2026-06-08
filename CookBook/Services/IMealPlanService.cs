using CookBook.Models;
using CookBook.ViewModels;

namespace CookBook.Services;

public interface IMealPlanService
{
    /// <summary>Buduje widok tygodnia (7 dni × sloty) zaczynającego się od poniedziałku zawierającego podaną datę.</summary>
    Task<MealPlanWeekViewModel> GetWeekAsync(int userId, DateTime anyDayInWeek);

    Task<(bool Success, string? Error)> AddAsync(int userId, DateTime date, MealType mealType, int recipeId);
    Task<(bool Success, string? Error)> RemoveAsync(int userId, int itemId);
}
