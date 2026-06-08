namespace CookBook.Models;

public class ShoppingListItem
{
    public int ShoppingListId { get; set; }
    public ShoppingList ShoppingList { get; set; } = null!;

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public double Amount { get; set; }

    public bool IsChecked { get; set; }
}
