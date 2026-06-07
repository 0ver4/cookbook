using System.Linq.Expressions;
using CookBook.Data;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Repositories;

/// <summary>
/// Domyślna implementacja repozytorium oparta na EF Core.
/// Po tej klasie dziedziczą repozytoria konkretnych encji (własne zapytania).
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly CookBookContext Context;
    protected readonly DbSet<T> Set;

    public Repository(CookBookContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id) => await Set.FindAsync(id);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync() =>
        await Set.AsNoTracking().ToListAsync();

    public virtual async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate) =>
        await Set.AsNoTracking().Where(predicate).ToListAsync();

    public IQueryable<T> Query() => Set.AsQueryable();

    public virtual async Task AddAsync(T entity) => await Set.AddAsync(entity);

    public virtual void Update(T entity) => Set.Update(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);

    public virtual async Task<bool> ExistsAsync(int id) => await GetByIdAsync(id) is not null;

    public async Task<int> SaveChangesAsync() => await Context.SaveChangesAsync();
}
