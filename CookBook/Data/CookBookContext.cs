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

    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeStep> RecipeSteps { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<RecipeCategory> RecipeCategories { get; set; }
    public DbSet<RecipeTag> RecipeTags { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<RecipeImage> RecipeImages { get; set; }

    public DbSet<Comment> Comments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Reaction> Reactions { get; set; }
    public DbSet<CommentReaction> CommentReactions { get; set; }

    public DbSet<Collection> Collections { get; set; }
    public DbSet<RecipeToCollection> RecipeToCollections { get; set; }
    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingListItem> ShoppingListItems { get; set; }
    public DbSet<MealPlanItem> MealPlanItems { get; set; }

    public DbSet<RecipeReport> RecipeReports { get; set; }
    public DbSet<CommentReport> CommentReports { get; set; }
    public DbSet<NotificationType> NotificationTypes { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Dictionaries: unique names ---
        modelBuilder.Entity<DifficultyLevel>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Unit>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Tag>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Allergen>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<NutritionType>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Ingredient>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Reaction>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<NotificationType>().HasIndex(n => n.Name).IsUnique();

        // --- Composite keys for join entities ---
        modelBuilder.Entity<IngredientAllergen>().HasKey(ia => new { ia.IngredientId, ia.AllergenId });
        modelBuilder.Entity<IngredientNutrition>().HasKey(inn => new { inn.IngredientId, inn.NutritionTypeId });
        modelBuilder.Entity<RecipeIngredient>().HasKey(ri => new { ri.RecipeId, ri.IngredientId });
        modelBuilder.Entity<RecipeCategory>().HasKey(rc => new { rc.RecipeId, rc.CategoryId });
        modelBuilder.Entity<RecipeTag>().HasKey(rt => new { rt.RecipeId, rt.TagId });
        modelBuilder.Entity<RecipeStep>().HasKey(rs => new { rs.RecipeId, rs.Order });
        modelBuilder.Entity<RecipeImage>().HasKey(ri => new { ri.RecipeId, ri.ImageId });
        modelBuilder.Entity<RecipeImage>().HasIndex(ri => new { ri.RecipeId, ri.Order }).IsUnique();
        modelBuilder.Entity<Review>().HasKey(r => new { r.RecipeId, r.UserId });
        modelBuilder.Entity<RecipeToCollection>().HasKey(rc => new { rc.RecipeId, rc.CollectionId });
        modelBuilder.Entity<ShoppingListItem>().HasKey(si => new { si.ShoppingListId, si.IngredientId, si.UnitId });
        modelBuilder.Entity<CommentReaction>().HasKey(cr => new { cr.CommentId, cr.UserId });

        // --- Recipe children: cascade from Recipe ---
        modelBuilder.Entity<RecipeStep>()
            .HasOne(rs => rs.Recipe).WithMany(r => r.Steps)
            .HasForeignKey(rs => rs.RecipeId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeImage>()
            .HasOne(ri => ri.Recipe).WithMany(r => r.Images)
            .HasForeignKey(ri => ri.RecipeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecipeImage>()
            .HasOne(ri => ri.Image).WithMany()
            .HasForeignKey(ri => ri.ImageId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Recipe).WithMany(r => r.Ingredients)
            .HasForeignKey(ri => ri.RecipeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Ingredient).WithMany()
            .HasForeignKey(ri => ri.IngredientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Unit).WithMany()
            .HasForeignKey(ri => ri.UnitId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeCategory>()
            .HasOne(rc => rc.Recipe).WithMany(r => r.Categories)
            .HasForeignKey(rc => rc.RecipeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecipeCategory>()
            .HasOne(rc => rc.Category).WithMany()
            .HasForeignKey(rc => rc.CategoryId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipeTag>()
            .HasOne(rt => rt.Recipe).WithMany(r => r.Tags)
            .HasForeignKey(rt => rt.RecipeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecipeTag>()
            .HasOne(rt => rt.Tag).WithMany()
            .HasForeignKey(rt => rt.TagId).OnDelete(DeleteBehavior.Restrict);

        // --- Recipe / User relationships (Restrict to avoid multiple cascade paths) ---
        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.User).WithMany()
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.DifficultyLevel).WithMany()
            .HasForeignKey(r => r.DifficultyLevelId).OnDelete(DeleteBehavior.Restrict);
        // Trigger INSTEAD OF DELETE (kasuje komentarze przepisu, potem przepis) →
        // EF musi używać SQL bez klauzuli OUTPUT przy zapisach do Recipes.
        modelBuilder.Entity<Recipe>()
            .ToTable(t => t.HasTrigger("trg_Recipes_DeleteCascadeComments"));

        modelBuilder.Entity<Image>()
            .HasOne(i => i.UploadedBy).WithMany()
            .HasForeignKey(i => i.UploadedById).OnDelete(DeleteBehavior.Restrict);

        // --- Comments ---
        // NO ACTION (nie Cascade): SQL Server nie pozwala na trigger INSTEAD OF DELETE
        // na tabeli z kaskadowym FK. Usuwanie komentarzy przepisu przejmuje
        // trigger INSTEAD OF DELETE na Recipes (trg_Recipes_DeleteCascadeComments).
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Recipe).WithMany(r => r.Comments)
            .HasForeignKey(c => c.RecipeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Image).WithMany()
            .HasForeignKey(c => c.ImageId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ReplyTo).WithMany(c => c.Replies)
            .HasForeignKey(c => c.ReplyToId).OnDelete(DeleteBehavior.Restrict);
        // Tabela ma triggery (kaskadowe usuwanie poddrzewa + tworzenie powiadomień),
        // więc EF musi przełączyć się na SQL bez klauzuli OUTPUT.
        modelBuilder.Entity<Comment>()
            .ToTable(t =>
            {
                t.HasTrigger("trg_Comments_DeleteCascade");
                t.HasTrigger("trg_Comments_AfterInsert_Notify");
            });

        // --- Reviews ---
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Recipe).WithMany(r => r.Reviews)
            .HasForeignKey(r => r.RecipeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Review>()
            .HasOne(r => r.User).WithMany()
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>()
            .ToTable(t => t.HasCheckConstraint("chk_review_rating_range", "[Rating] BETWEEN 1 AND 5"));

        // --- Reactions ---
        modelBuilder.Entity<Reaction>()
            .HasOne(r => r.Image).WithMany()
            .HasForeignKey(r => r.ImageId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CommentReaction>()
            .HasOne(cr => cr.Comment).WithMany(c => c.Reactions)
            .HasForeignKey(cr => cr.CommentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CommentReaction>()
            .HasOne(cr => cr.User).WithMany()
            .HasForeignKey(cr => cr.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CommentReaction>()
            .HasOne(cr => cr.Reaction).WithMany()
            .HasForeignKey(cr => cr.ReactionId).OnDelete(DeleteBehavior.Restrict);

        // --- Collections ---
        modelBuilder.Entity<Collection>()
            .HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RecipeToCollection>()
            .HasOne(rc => rc.Collection).WithMany(c => c.Recipes)
            .HasForeignKey(rc => rc.CollectionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecipeToCollection>()
            .HasOne(rc => rc.Recipe).WithMany()
            .HasForeignKey(rc => rc.RecipeId).OnDelete(DeleteBehavior.Cascade);

        // --- Shopping lists ---
        modelBuilder.Entity<ShoppingList>()
            .HasOne(s => s.User).WithMany()
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ShoppingListItem>()
            .HasOne(si => si.ShoppingList).WithMany(s => s.Items)
            .HasForeignKey(si => si.ShoppingListId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ShoppingListItem>()
            .HasOne(si => si.Ingredient).WithMany()
            .HasForeignKey(si => si.IngredientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ShoppingListItem>()
            .HasOne(si => si.Unit).WithMany()
            .HasForeignKey(si => si.UnitId).OnDelete(DeleteBehavior.Restrict);

        // --- Meal plan ---
        modelBuilder.Entity<MealPlanItem>()
            .HasOne(m => m.User).WithMany()
            .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MealPlanItem>()
            .HasOne(m => m.Recipe).WithMany()
            .HasForeignKey(m => m.RecipeId).OnDelete(DeleteBehavior.Cascade);

        // --- Reports ---
        modelBuilder.Entity<RecipeReport>()
            .HasOne(r => r.Recipe).WithMany()
            .HasForeignKey(r => r.RecipeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecipeReport>()
            .HasOne(r => r.ReportedBy).WithMany()
            .HasForeignKey(r => r.ReportedById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RecipeReport>()
            .HasOne(r => r.ResolvedBy).WithMany()
            .HasForeignKey(r => r.ResolvedById).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CommentReport>()
            .HasOne(r => r.Comment).WithMany()
            .HasForeignKey(r => r.CommentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CommentReport>()
            .HasOne(r => r.ReportedBy).WithMany()
            .HasForeignKey(r => r.ReportedById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CommentReport>()
            .HasOne(r => r.ResolvedBy).WithMany()
            .HasForeignKey(r => r.ResolvedById).OnDelete(DeleteBehavior.Restrict);

        // --- Notifications (zawsze celują w komentarz; kaskada z komentarza/przepisu) ---
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User).WithMany()
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.NotificationType).WithMany()
            .HasForeignKey(n => n.NotificationTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.TriggeredByUser).WithMany()
            .HasForeignKey(n => n.TriggeredByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Comment).WithMany()
            .HasForeignKey(n => n.CommentId).OnDelete(DeleteBehavior.Cascade);

        // --- Seed data ---
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

        modelBuilder.Entity<NotificationType>().HasData(
            new NotificationType { Id = 1, Name = "NowyKomentarz" },
            new NotificationType { Id = 2, Name = "OdpowiedzNaKomentarz" },
            new NotificationType { Id = 3, Name = "NowaRecenzja" },
            new NotificationType { Id = 4, Name = "ZgloszenieRozpatrzone" }
        );
    }
}
