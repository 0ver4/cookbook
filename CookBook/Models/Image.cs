using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class Image
{
    public int Id { get; set; }

    // Wgrane pliki: zawartość trzymana w bazie (blob), synchronizuje się między środowiskami
    public byte[]? Data { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    // Zewnętrzne obrazki (np. ikony reakcji): adres zamiast blobu
    [MaxLength(500)]
    public string? Url { get; set; }

    public int UploadedById { get; set; }
    public ApplicationUser UploadedBy { get; set; } = null!;
}
