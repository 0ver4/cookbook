using CookBook.Data;
using CookBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(CookBookContext context) : base(context)
    {
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null) =>
        await Set.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId));
}
