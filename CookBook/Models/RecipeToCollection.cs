namespace CookBook.Models;

public class RecipeToCollection
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public int CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;
}
