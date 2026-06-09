using CookBook.Data;
using CookBook.Dtos;
using CookBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Repositories;

public class RecipeRepository : Repository<Recipe>, IRecipeRepository
{
    public RecipeRepository(CookBookContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Recipe>> GetListAsync(RecipeQuery? query = null)
    {
        var q = Set.AsNoTracking()
            .Where(r => r.IsPublished && !r.IsHidden)
            .Include(r => r.User)
            .Include(r => r.DifficultyLevel)
            .Include(r => r.Images.OrderBy(i => i.Order))
            .Include(r => r.Reviews)
            .Include(r => r.Categories).ThenInclude(c => c.Category)
            .AsQueryable();

        if (query is not null)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
                q = q.Where(r => r.Name.Contains(query.Search));

            if (query.CategoryId.HasValue)
                q = q.Where(r => r.Categories.Any(c => c.CategoryId == query.CategoryId.Value));

            if (query.DifficultyId.HasValue)
                q = q.Where(r => r.DifficultyLevelId == query.DifficultyId.Value);

            q = query.Sort switch
            {
                "rating" => q.OrderByDescending(r => r.Reviews.Any()
                    ? r.Reviews.Average(rv => (double)rv.Rating) : 0),
                "name"   => q.OrderBy(r => r.Name),
                _        => q.OrderByDescending(r => r.CreatedAt) // "newest"
            };
        }
        else
        {
            q = q.OrderByDescending(r => r.CreatedAt);
        }

        return await q.ToListAsync();
    }

    public async Task<Recipe?> GetDetailsAsync(int id)
    {
        return await Set.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.DifficultyLevel)
            .Include(r => r.Images)
            .Include(r => r.Steps.OrderBy(s => s.Order))
            .Include(r => r.Ingredients).ThenInclude(i => i.Ingredient).ThenInclude(ing => ing.Unit)
            .Include(r => r.Ingredients).ThenInclude(i => i.Unit)
            .Include(r => r.Categories).ThenInclude(c => c.Category)
            .Include(r => r.Tags).ThenInclude(t => t.Tag)
            .Include(r => r.Reviews)
            .Include(r => r.Comments).ThenInclude(c => c.User)
            .Include(r => r.Comments).ThenInclude(c => c.Reactions).ThenInclude(cr => cr.Reaction)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Recipe?> GetForEditAsync(int id)
    {
        return await Set
            .Include(r => r.Steps)
            .Include(r => r.Ingredients).ThenInclude(i => i.Ingredient)
            .Include(r => r.Categories)
            .Include(r => r.Tags)
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
