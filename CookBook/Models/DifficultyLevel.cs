using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class DifficultyLevel
{
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;
}