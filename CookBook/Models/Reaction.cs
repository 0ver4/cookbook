using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class Reaction
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;

    public int ImageId { get; set; }
    public Image Image { get; set; } = null!;
}
