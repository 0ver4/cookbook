using CookBook.Data;
using CookBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Repositories;

/// <summary>Repozytorium słownika oparte na EF Core.</summary>
public class LookupRepository<T> : Repository<T>, ILookupRepository<T> where T : class, INamedEntity
{
    public LookupRepository(CookBookContext context) : base(context) { }

    // EF.Property pozwala odwołać się do kolumny po nazwie — tłumaczy się zawsze na WHERE Name = @p,
    // bez problemu z dostępem do składowej przez interfejs. excludeId = 0 (Id z identity nigdy nie jest 0)
    // oznacza "nic nie wykluczaj" przy tworzeniu.
    public Task<bool> ExistsByNameAsync(string name, int excludeId = 0) =>
        Set.AnyAsync(x =>
            EF.Property<int>(x, "Id") != excludeId &&
            EF.Property<string>(x, "Name") == name);
}
