using CookBook.Data;
using CookBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Repositories;

public class MealPlanRepository : Repository<MealPlanItem>, IMealPlanRepository
{
    public MealPlanRepository(CookBookContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<MealPlanItem>> GetForUserInRangeAsync(int userId, DateTime from, DateTime to)
    {
        return await Set.AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= from && m.Date < to)
            .Include(m => m.Recipe)
            .OrderBy(m => m.Date)
            .ToListAsync();
    }
}
