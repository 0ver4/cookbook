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
    public DbSet<Unit> Units { get; set; } 
    public DbSet<Category> Categories { get; set; } 
    public DbSet<Tag> Tags { get; set; } 
    public DbSet<Allergen> Allergens { get; set; } 
    public DbSet<NutritionType> NutritionTypes { get; set; } 
    public DbSet<Ingredient> Ingredients { get; set; } 
    public DbSet<IngredientAllergen> IngredientAllergens { get; set; } 
    public DbSet<IngredientNutrition> IngredientNutritions { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DifficultyLevel>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Unit>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Tag>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Allergen>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<NutritionType>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Ingredient>().HasIndex(d => d.Name).IsUnique();

        modelBuilder.Entity<DifficultyLevel>().HasData(
            new DifficultyLevel { Id = 1, Name = "Łatwy" },
            new DifficultyLevel { Id = 2, Name = "Średni" },
            new DifficultyLevel { Id = 3, Name = "Trudny" }
        );
        
        modelBuilder.Entity<Unit>().HasData(
            new Unit { Id = 1, Name = "gram" },
            new Unit { Id = 2, Name = "mililitr" },
            new Unit { Id = 3, Name = "sztuka" },
            new Unit { Id = 4, Name = "szklanka" },
            new Unit { Id = 5, Name = "łyżeczka" },
            new Unit { Id = 6, Name = "łyżka" }
        );
        
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Zupa" },
            new Category { Id = 2, Name = "Danie główne" },
            new Category { Id = 3, Name = "Deser" },
            new Category { Id = 4, Name = "Śniadanie" },
            new Category { Id = 5, Name = "Obiad" },
            new Category { Id = 6, Name = "Kolacja" },
            new Category { Id = 7, Name = "Przekąska" }
        );
        
        modelBuilder.Entity<Tag>().HasData(
            new Tag { Id = 1, Name = "Wegetariańskie" },
            new Tag { Id = 2, Name = "Wegańskie" },
            new Tag { Id = 3, Name = "Bezglutenowe" },
            new Tag { Id = 4, Name = "Dla dzieci" },
            new Tag { Id = 5, Name = "Szybkie" },
            new Tag { Id = 6, Name = "Na imprezę" },
            new Tag { Id = 7, Name = "Słodkie" },
            new Tag { Id = 8, Name = "Słone" },
            new Tag { Id = 9, Name = "Ostre" }
        );
        
        modelBuilder.Entity<Allergen>().HasData(
            new Allergen { Id = 1, Name = "Gluten" },
            new Allergen { Id = 2, Name = "Orzechy" },
            new Allergen { Id = 3, Name = "Mleko" },
            new Allergen { Id = 4, Name = "Jaja" },
            new Allergen { Id = 5, Name = "Ryby" },
            new Allergen { Id = 6, Name = "Skorupiaki" },
            new Allergen { Id = 7, Name = "Pomidory" }
        );
        
        modelBuilder.Entity<NutritionType>().HasData(
            new NutritionType { Id = 1, Name = "Kalorie" },
            new NutritionType { Id = 2, Name = "Białko" },
            new NutritionType { Id = 3, Name = "Tłuszcze" },
            new NutritionType { Id = 4, Name = "Węglowodany" },
            new NutritionType { Id = 5, Name = "Błonnik" },
            new NutritionType { Id = 6, Name = "Cukry" }
        );
        
        modelBuilder.Entity<IngredientAllergen>().HasKey(ia => new { ia.IngredientId, ia.AllergenId });
        
        modelBuilder.Entity<IngredientNutrition>().HasKey(inn => new { inn.IngredientId, inn.NutritionTypeId });
        
        
        base.OnModelCreating(modelBuilder);
    }
}