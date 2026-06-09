using Microsoft.AspNetCore.Identity;

namespace CookBook.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime AccountCreated { get; set; } = DateTime.UtcNow;

    // Publiczna nazwa wyświetlana: imię+nazwisko jeśli ustawione, wpp. UserName, wpp. Email
    public string PublicUsername
    {
        get
        {
            var fullName = $"{FirstName} {LastName}".Trim();
            return !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : (UserName ?? Email ?? "Użytkownik");
        }
    }
}
