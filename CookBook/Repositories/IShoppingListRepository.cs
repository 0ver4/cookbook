using CookBook.Models;

namespace CookBook.Repositories;

public interface IShoppingListRepository : IRepository<ShoppingList>
{
    /// <summary>Listy zakupów użytkownika wraz z pozycjami (do policzenia odhaczonych).</summary>
    Task<IReadOnlyList<ShoppingList>> GetForUserAsync(int userId);

    /// <summary>Pojedyncza lista z pozycjami, składnikami i jednostkami - śledzona (do edycji pozycji).</summary>
    Task<ShoppingList?> GetWithItemsAsync(int id);
}
