namespace CookBook.Models;

// Encja bezkluczowa mapowana na widok vw_RecipeRatings.
// Liczbę recenzji i średnią ocenę liczy baza, nie aplikacja.
public class RecipeRating
{
    public int RecipeId { get; set; }
    public int ReviewCount { get; set; }
    public double? AverageRating { get; set; }
}
