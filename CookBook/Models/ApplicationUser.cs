using Microsoft.AspNetCore.Identity;

namespace CookBook.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime AccountCreated { get; set; } = DateTime.Now;
    }
}