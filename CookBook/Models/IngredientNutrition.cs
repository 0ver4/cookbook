namespace CookBook.Models;

public class IngredientNutrition
{
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    public int NutritionTypeId { get; set; }
    public NutritionType NutritionType { get; set; } = null!;

    public double AmountPer100g { get; set; }
}