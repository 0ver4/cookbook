using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class Recipe
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }

    public bool IsPublished { get; set; } = true;
    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int DifficultyLevelId { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; } = null!;

    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    public ICollection<RecipeImage> Images { get; set; } = new List<RecipeImage>();
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RecipeCategory> Categories { get; set; } = new List<RecipeCategory>();
    public ICollection<RecipeTag> Tags { get; set; } = new List<RecipeTag>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
