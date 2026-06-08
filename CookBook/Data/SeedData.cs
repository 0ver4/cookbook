using Microsoft.AspNetCore.Identity;
using CookBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        
        string[] roleNames = { "User", "Moderator" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
        }

        // --- MIEJSCE NA SEEDOWANIE REAKCJI ---
        var context = serviceProvider.GetRequiredService<CookBookContext>();
        
        // Zanim dodamy reakcje z obrazkami, upewnijmy się, że w ogóle mamy w bazie jakiegoś systemowego usera,
        // do którego "przypiszemy" wgrane obrazki uśmieszków (wymagane w Image.cs: UploadedById).
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var systemUser = await userManager.FindByEmailAsync("system@cookbook.local");
        
        if (systemUser == null)
        {
            systemUser = new ApplicationUser
            {
                UserName = "system@cookbook.local",
                Email = "system@cookbook.local",
                FirstName = "System",
                LastName = "Bot",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(systemUser, "System!123");
            await userManager.AddToRoleAsync(systemUser, "Moderator");
        }

        if (!await context.Reactions.AnyAsync())
        {
            // Seed image urls dla emojis
            var thumbUpImg = new Image { Url = "https://cdn-icons-png.flaticon.com/512/1183/1183183.png", UploadedById = systemUser.Id };
            var heartImg = new Image { Url = "https://cdn-icons-png.flaticon.com/512/833/833472.png", UploadedById = systemUser.Id };
            var yummyImg = new Image { Url = "https://cdn-icons-png.flaticon.com/512/3673/3673574.png", UploadedById = systemUser.Id };
            var thumbDownImg = new Image { Url = "https://cdn-icons-png.flaticon.com/512/1183/1183196.png", UploadedById = systemUser.Id };

            context.Images.AddRange(thumbUpImg, heartImg, yummyImg, thumbDownImg);
            await context.SaveChangesAsync();

            context.Reactions.AddRange(
                new Reaction { Name = "Lubię to", ImageId = thumbUpImg.Id },
                new Reaction { Name = "Serduszko", ImageId = heartImg.Id },
                new Reaction { Name = "Pyszne", ImageId = yummyImg.Id },
                new Reaction { Name = "Nie lubię", ImageId = thumbDownImg.Id }
            );

            await context.SaveChangesAsync();
        }
    }
}   // dane startowe do bazy, czy w bazie istnieją wymagane role tworzy je jeśli ich brakuje