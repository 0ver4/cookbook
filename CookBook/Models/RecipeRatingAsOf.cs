namespace CookBook.Models;

// Wynik funkcji fn_RecipeAvgRatingAsOf — średnia ocena przepisu w danym punkcie w czasie.
public class RecipeRatingAsOf
{
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
