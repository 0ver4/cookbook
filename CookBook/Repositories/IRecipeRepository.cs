using CookBook.Dtos;
using CookBook.Models;

namespace CookBook.Repositories;

public interface IRecipeRepository : IRepository<Recipe>
{
    /// <summary>Strona przepisów z filtrowaniem, sortowaniem i danymi na listę (stronicowanie po stronie bazy).</summary>
    Task<PagedResult<Recipe>> GetListAsync(RecipeQuery? query = null);

    /// <summary>Pełny przepis ze wszystkimi powiązaniami do widoku szczegółów.</summary>
    Task<Recipe?> GetDetailsAsync(int id);

    /// <summary>Przepis z kolekcjami edytowalnymi (kroki, składniki, kategorie, tagi, zdjęcia) - śledzony.</summary>
    Task<Recipe?> GetForEditAsync(int id);
}
