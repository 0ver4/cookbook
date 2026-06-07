using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class ShoppingList
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ShoppingListItem> Items { get; set; } = new List<ShoppingListItem>();
}
