using CookBook.Models;

namespace CookBook.Repositories;

/// <summary>
/// Repozytorium słownika { Id, Name }. Rozszerza IRepository o sprawdzanie unikalności nazwy,
/// które wymaga tłumaczenia zapytania przez EF (EF.Property) — dzięki wydzieleniu na interfejs
/// jest podmienialne w testach jednostkowych LookupService.
/// </summary>
public interface ILookupRepository<T> : IRepository<T> where T : class, INamedEntity
{
    /// <summary>Czy istnieje wpis o podanej nazwie (opcjonalnie z pominięciem jednego Id przy edycji)?</summary>
    Task<bool> ExistsByNameAsync(string name, int excludeId = 0);
}
