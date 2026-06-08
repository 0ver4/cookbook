using CookBook.Data;
using CookBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Repositories;

public class RecipeRepository : Repository<Recipe>, IRecipeRepository
{
    public RecipeRepository(CookBookContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Recipe>> GetListAsync()
    {
        return await Set.AsNoTracking()
            .Where(r => r.IsPublished && !r.IsHidden)
            .Include(r => r.User)
            .Include(r => r.DifficultyLevel)
            .Include(r => r.Images.OrderBy(i => i.Order)).ThenInclude(i => i.Image)
            .Include(r => r.Reviews)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Recipe?> GetDetailsAsync(int id)
    {
        return await Set.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.DifficultyLevel)
            .Include(r => r.Images.OrderBy(i => i.Order)).ThenInclude(i => i.Image)
            .Include(r => r.Steps.OrderBy(s => s.Order))
            .Include(r => r.Ingredients).ThenInclude(i => i.Ingredient).ThenInclude(ing => ing.Unit)
            .Include(r => r.Ingredients).ThenInclude(i => i.Unit)
            .Include(r => r.Categories).ThenInclude(c => c.Category)
            .Include(r => r.Tags).ThenInclude(t => t.Tag)
            .Include(r => r.Reviews)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Recipe?> GetForEditAsync(int id)
    {
        return await Set
            .Include(r => r.Steps)
            .Include(r => r.Ingredients).ThenInclude(i => i.Ingredient)
            .Include(r => r.Categories)
            .Include(r => r.Tags)
            .Include(r => r.Images).ThenInclude(i => i.Image)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
