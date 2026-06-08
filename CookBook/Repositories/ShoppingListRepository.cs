using CookBook.Data;
using CookBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Repositories;

public class ShoppingListRepository : Repository<ShoppingList>, IShoppingListRepository
{
    public ShoppingListRepository(CookBookContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ShoppingList>> GetForUserAsync(int userId)
    {
        return await Set.AsNoTracking()
            .Where(s => s.UserId == userId)
            .Include(s => s.Items)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<ShoppingList?> GetWithItemsAsync(int id)
    {
        return await Set
            .Include(s => s.Items).ThenInclude(i => i.Ingredient)
            .Include(s => s.Items).ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}
