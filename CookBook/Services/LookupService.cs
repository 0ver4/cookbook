using CookBook.Models;
using CookBook.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Services;

/// <summary>
/// Niegeneryczna fasada nad słownikiem — pozwala kontrolerowi operować na słowniku
/// bez znajomości konkretnego typu encji w czasie kompilacji (typ wynika ze slug-a).
/// </summary>
public interface ILookupOps
{
    Task<IReadOnlyList<(int Id, string Name)>> GetAllAsync();
    Task<(int Id, string Name)?> GetByIdAsync(int id);
    Task<int> CountAsync();
    Task<(bool Success, string? Error)> CreateAsync(string name);
    Task<(bool Success, string? Error)> UpdateAsync(int id, string name);
    Task DeleteAsync(int id);
}

/// <summary>Obsługa CRUD dla dowolnego prostego słownika { Id, Name }.</summary>
public class LookupService<T> : ILookupOps where T : class, INamedEntity, new()
{
    private const int MaxNameLength = 50;
    private readonly ILookupRepository<T> _repo;

    public LookupService(ILookupRepository<T> repo) => _repo = repo;

    public async Task<IReadOnlyList<(int, string)>> GetAllAsync() =>
        (await _repo.GetAllAsync())
            .OrderBy(x => x.Name)
            .Select(x => (x.Id, x.Name))
            .ToList();

    public async Task<(int, string)?> GetByIdAsync(int id) =>
        await _repo.GetByIdAsync(id) is { } e ? (e.Id, e.Name) : null;

    public Task<int> CountAsync() => _repo.Query().CountAsync();

    public async Task<(bool, string?)> CreateAsync(string name)
    {
        var (norm, error) = Normalize(name);
        if (error is not null) return (false, error);
        if (await _repo.ExistsByNameAsync(norm!)) return (false, "Taka nazwa już istnieje.");

        await _repo.AddAsync(new T { Name = norm! });
        await _repo.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool, string?)> UpdateAsync(int id, string name)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return (false, "Nie znaleziono pozycji.");

        var (norm, error) = Normalize(name);
        if (error is not null) return (false, error);
        if (await _repo.ExistsByNameAsync(norm!, excludeId: id)) return (false, "Taka nazwa już istnieje.");

        entity.Name = norm!;
        _repo.Update(entity);
        await _repo.SaveChangesAsync();
        return (true, null);
    }

    public async Task DeleteAsync(int id)
    {
        if (await _repo.GetByIdAsync(id) is { } e)
        {
            _repo.Remove(e);
            await _repo.SaveChangesAsync();
        }
    }

    private static (string?, string?) Normalize(string name)
    {
        var trimmed = name?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(trimmed)) return (null, "Nazwa jest wymagana.");
        if (trimmed.Length > MaxNameLength) return (null, $"Nazwa jest zbyt długa (maksymalnie {MaxNameLength} znaków).");
        return (trimmed, null);
    }
}
