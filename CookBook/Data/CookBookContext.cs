using CookBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Data;

public class CookBookContext : DbContext
{
    public CookBookContext(DbContextOptions<CookBookContext> options)
        : base(options)
    {
    }
    
    public DbSet<DifficultyLevel> DifficultyLevels { get; set; } 
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DifficultyLevel>().HasIndex(d => d.Name).IsUnique();
        base.OnModelCreating(modelBuilder);
    }
}