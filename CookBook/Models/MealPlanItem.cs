namespace CookBook.Models;

public class MealPlanItem
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public DateTime Date { get; set; }

    public MealType MealType { get; set; }
}
