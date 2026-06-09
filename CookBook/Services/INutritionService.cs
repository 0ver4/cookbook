using CookBook.Models;

namespace CookBook.Services;

public interface INutritionService
{
    /// <summary>
    /// Pobiera wartości odżywcze dla nowo tworzonego składnika i dopina je do jego
    /// kolekcji IngredientNutritions. Nigdy nie rzuca — w razie problemu nic nie dopina.
    /// </summary>
    Task PopulateNutritionAsync(Ingredient ingredient, CancellationToken ct = default);
}
