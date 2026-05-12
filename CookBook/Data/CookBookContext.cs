using CookBook.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Data;

public class CookBookContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public CookBookContext(DbContextOptions<CookBookContext> options)
        : base(options)
    {
    }
    
    public DbSet<DifficultyLevel> DifficultyLevels { get; set; } 
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<DifficultyLevel>().HasIndex(d => d.Name).IsUnique();
    }
}