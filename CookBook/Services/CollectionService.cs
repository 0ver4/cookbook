using CookBook.Data;
using CookBook.Dtos;
using CookBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Services;

public class CollectionService : ICollectionService
{
    private readonly CookBookContext _db;

    public CollectionService(CookBookContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CollectionSummaryDto>> GetForUserAsync(int userId)
    {
        return await _db.Collections
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CollectionSummaryDto(
                c.Id,
                c.Name,
                c.CreatedAt,
                c.Recipes.Count))
            .ToListAsync();
    }

    public async Task<CollectionDetailsDto?> GetDetailsAsync(int id, int userId)
    {
        var col = await _db.Collections
            .Include(c => c.Recipes)
                .ThenInclude(rc => rc.Recipe)
                    .ThenInclude(r => r.DifficultyLevel)
            .Include(c => c.Recipes)
                .ThenInclude(rc => rc.Recipe)
                    .ThenInclude(r => r.Images)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (col is null) return null;

        var recipes = col.Recipes
            .OrderBy(rc => rc.RecipeId)
            .Select(rc =>
            {
                var r = rc.Recipe;
                var firstImageId = r.Images.OrderBy(i => i.Order).Select(i => (int?)i.ImageId).FirstOrDefault();
                return new CollectionRecipeDto(
                    r.Id,
                    r.Name,
                    firstImageId.HasValue ? $"/Image/{firstImageId}" : null,
                    r.DifficultyLevel.Name);
            })
            .ToList();

        return new CollectionDetailsDto(col.Id, col.Name, col.CreatedAt, recipes);
    }

    public async Task<(bool Success, string? Error, int CollectionId)> CreateAsync(int userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Podaj nazwę kolekcji.", 0);

        var col = new Collection { UserId = userId, Name = name.Trim(), CreatedAt = DateTime.UtcNow };
        _db.Collections.Add(col);
        await _db.SaveChangesAsync();
        return (true, null, col.Id);
    }

    public async Task<(bool Success, string? Error)> RenameAsync(int id, int userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Podaj nazwę kolekcji.");

        var col = await _db.Collections.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (col is null) return (false, "Nie znaleziono kolekcji.");

        col.Name = name.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, int userId)
    {
        var col = await _db.Collections.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (col is null) return (false, "Nie znaleziono kolekcji.");

        _db.Collections.Remove(col);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddRecipeAsync(int collectionId, int userId, int recipeId)
    {
        var col = await _db.Collections.FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId);
        if (col is null) return (false, "Nie znaleziono kolekcji.");

        var exists = await _db.RecipeToCollections.AnyAsync(rc => rc.CollectionId == collectionId && rc.RecipeId == recipeId);
        if (exists) return (false, "Przepis już jest w tej kolekcji.");

        _db.RecipeToCollections.Add(new RecipeToCollection { CollectionId = collectionId, RecipeId = recipeId });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveRecipeAsync(int collectionId, int userId, int recipeId)
    {
        var col = await _db.Collections.FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId);
        if (col is null) return (false, "Nie znaleziono kolekcji.");

        var link = await _db.RecipeToCollections.FirstOrDefaultAsync(rc => rc.CollectionId == collectionId && rc.RecipeId == recipeId);
        if (link is null) return (false, "Przepis nie jest w tej kolekcji.");

        _db.RecipeToCollections.Remove(link);
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
