namespace CookBook.Models;

public class RecipeImage
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public int ImageId { get; set; }
    public Image Image { get; set; } = null!;

    public int Order { get; set; }
}
