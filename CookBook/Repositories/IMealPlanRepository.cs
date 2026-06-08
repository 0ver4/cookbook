using CookBook.Models;

namespace CookBook.Repositories;

public interface IMealPlanRepository : IRepository<MealPlanItem>
{
    /// <summary>Pozycje planu danego użytkownika w zakresie dat (z dołączonym przepisem).</summary>
    Task<IReadOnlyList<MealPlanItem>> GetForUserInRangeAsync(int userId, DateTime from, DateTime to);
}
