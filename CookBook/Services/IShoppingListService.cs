using CookBook.Dtos;

namespace CookBook.Services;

public interface IShoppingListService
{
    Task<IReadOnlyList<ShoppingListSummaryDto>> GetForUserAsync(int userId);
    Task<ShoppingListDetailsDto?> GetDetailsAsync(int id, int userId);

    Task<(bool Success, string? Error, int ListId)> CreateAsync(int userId, string name);
    Task<(bool Success, string? Error)> DeleteAsync(int id, int userId);

    /// <summary>Dodaje pozycję po nazwie składnika (find-or-create); jeśli ten sam składnik w tej samej jednostce już jest, sumuje ilość. Pusta jednostka = domyślna jednostka składnika.</summary>
    Task<(bool Success, string? Error)> AddItemAsync(int listId, int userId, string ingredientName, double amount, int? unitId);
    Task<(bool Success, string? Error)> RemoveItemAsync(int listId, int userId, int ingredientId, int unitId);
    Task<(bool Success, string? Error)> ToggleItemAsync(int listId, int userId, int ingredientId, int unitId);

    /// <summary>Dokłada do listy składniki z przepisu (sumuje z istniejącymi pozycjami).</summary>
    Task<(bool Success, string? Error)> GenerateFromRecipeAsync(int listId, int userId, int recipeId);
}
