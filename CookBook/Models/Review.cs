using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class Review
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
