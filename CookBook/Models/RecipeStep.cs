using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class RecipeStep
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public int Order { get; set; }

    [Required, MaxLength(2000)]
    public string Content { get; set; } = null!;
}
