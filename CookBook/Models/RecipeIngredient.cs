namespace CookBook.Models;

public class RecipeIngredient
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    public double Amount { get; set; }

    public int? UnitId { get; set; }
    public Unit? Unit { get; set; }
}
