namespace CookBook.Models;

// Wynik funkcji tabelarycznej fn_UserStats(@userId) — komplet statystyk użytkownika.
public class UserStats
{
    public int UserId { get; set; }
    public int RecipeCount { get; set; }
    public int PublishedRecipeCount { get; set; }
    public int CommentCount { get; set; }
    public int ReviewCount { get; set; }
    public double? AverageRatingGiven { get; set; }
    public int CollectionCount { get; set; }
    public int ShoppingListCount { get; set; }
    public int MealPlanItemCount { get; set; }
    public double? AverageRatingReceived { get; set; }
}
