using System.Linq.Expressions;

namespace CookBook.Repositories;

/// <summary>
/// Generyczny interfejs repozytorium dla encji z kluczem typu int.
/// Tabele łączące (join) obsługujemy przez ich encję główną, nie tutaj.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Zapytanie do składania (filtrowanie, stronicowanie, projekcja).</summary>
    IQueryable<T> Query();

    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);

    Task<bool> ExistsAsync(int id);
    Task<int> SaveChangesAsync();
}
