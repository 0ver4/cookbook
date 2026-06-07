using CookBook.Models;

namespace CookBook.Repositories;

public interface IRecipeRepository : IRepository<Recipe>
{
    /// <summary>Przepisy z danymi potrzebnymi na listę (autor, trudność, zdjęcia, oceny).</summary>
    Task<IReadOnlyList<Recipe>> GetListAsync();

    /// <summary>Pełny przepis ze wszystkimi powiązaniami do widoku szczegółów.</summary>
    Task<Recipe?> GetDetailsAsync(int id);

    /// <summary>Przepis z kolekcjami edytowalnymi (kroki, składniki, kategorie, tagi, zdjęcia) - śledzony.</summary>
    Task<Recipe?> GetForEditAsync(int id);
}
