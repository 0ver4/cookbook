namespace CookBook.Models;

// Wynik funkcji fn_RecipeNutrition - zsumowane wartości odżywcze przepisu.
// Wszystkie pola NULL, gdy przepisu nie da się policzyć (składnik bez przelicznika
// na gramy lub bez kompletu 6 wartości odżywczych).
public class RecipeNutritionRow
{
    public double? Calories { get; set; }
    public double? Protein { get; set; }
    public double? Fat { get; set; }
    public double? Carbs { get; set; }
    public double? Fiber { get; set; }
    public double? Sugar { get; set; }
}
