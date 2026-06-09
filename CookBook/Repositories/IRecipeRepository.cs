using CookBook.Dtos;
using CookBook.Models;

namespace CookBook.Repositories;

public interface IRecipeRepository : IRepository<Recipe>
{
    /// <summary>Przepisy z filtrowaniem, sortowaniem i danymi na listę.</summary>
    Task<IReadOnlyList<Recipe>> GetListAsync(RecipeQuery? query = null);

    /// <summary>Pełny przepis ze wszystkimi powiązaniami do widoku szczegółów.</summary>
    Task<Recipe?> GetDetailsAsync(int id);

    /// <summary>Przepis z kolekcjami edytowalnymi (kroki, składniki, kategorie, tagi, zdjęcia) - śledzony.</summary>
    Task<Recipe?> GetForEditAsync(int id);
}
