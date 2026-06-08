using CookBook.Dtos;

namespace CookBook.Services;

public interface IShoppingListPdfService
{
    /// <summary>Generuje plik PDF listy zakupów.</summary>
    byte[] Generate(ShoppingListDetailsDto list);
}
