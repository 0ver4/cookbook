using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class Image
{
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string Url { get; set; } = null!;

    public int UploadedById { get; set; }
    public ApplicationUser UploadedBy { get; set; } = null!;
}
