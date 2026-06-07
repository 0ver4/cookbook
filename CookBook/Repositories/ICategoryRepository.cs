using CookBook.Models;

namespace CookBook.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
}
