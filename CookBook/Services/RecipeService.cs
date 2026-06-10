using CookBook.Data;
using CookBook.Dtos;
using CookBook.Models;
using CookBook.Repositories;
using CookBook.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Services;

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _recipes;
    private readonly IImageService _imageService;
    private readonly IRepository<Image> _images;
    private readonly IRepository<DifficultyLevel> _difficultyLevels;
    private readonly IRepository<Ingredient> _ingredients;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<Category> _categories;
    private readonly IRepository<Tag> _tags;
    private readonly IRepository<Comment> _comments;
    private readonly IRepository<CommentReaction> _commentReactions;
    private readonly INutritionService _nutrition;
    private readonly CookBookContext _db;

    public RecipeService(
        IRecipeRepository recipes,
        IImageService imageService,
        IRepository<Image> images,
        IRepository<DifficultyLevel> difficultyLevels,
        IRepository<Ingredient> ingredients,
        IRepository<Unit> units,
        IRepository<Category> categories,
        IRepository<Tag> tags,
        IRepository<Comment> comments,
        IRepository<CommentReaction> commentReactions,
        INutritionService nutrition,
        CookBookContext db)
    {
        _recipes = recipes;
        _imageService = imageService;
        _images = images;
        _difficultyLevels = difficultyLevels;
        _ingredients = ingredients;
        _units = units;
        _categories = categories;
        _tags = tags;
        _comments = comments;
        _commentReactions = commentReactions;
        _nutrition = nutrition;
        _db = db;
    }

    // Adres przez który serwujemy zdjęcie z bazy (ImageController)
    private static string ImageUrl(int imageId) => $"/Image/{imageId}";

    public async Task<RecipeListViewModel> GetListAsync(RecipeQuery? query = null)
    {
        query ??= new RecipeQuery();
        var recipes = await _recipes.GetListAsync(query);

        // Oceny pobieramy z widoku vw_RecipeRatings (agregacja po stronie bazy).
        var recipeIds = recipes.Select(r => r.Id).ToList();
        var ratings = await _db.RecipeRatings
            .Where(rr => recipeIds.Contains(rr.RecipeId))
            .ToDictionaryAsync(rr => rr.RecipeId);

        var items = recipes.Select(r => new RecipeListItemDto(
            r.Id,
            r.Name,
            r.Images.OrderBy(i => i.Order).Select(i => (int?)i.ImageId).FirstOrDefault() is int firstId ? ImageUrl(firstId) : null,
            r.DifficultyLevel.Name,
            AuthorName(r.User),
            ratings.TryGetValue(r.Id, out var rt) ? rt.AverageRating : null,
            ratings.TryGetValue(r.Id, out var rc) ? rc.ReviewCount : 0,
            r.Categories.Select(c => c.Category.Name).ToList()
        )).ToList();

        var categories = (await _categories.GetAllAsync())
            .OrderBy(c => c.Name).Select(c => new LookupItem(c.Id, c.Name)).ToList();
        var difficulties = (await _difficultyLevels.GetAllAsync())
            .OrderBy(d => d.Id).Select(d => new LookupItem(d.Id, d.Name)).ToList();

        return new RecipeListViewModel(items, categories, difficulties, query);
    }

    public async Task<RecipeDetailsDto?> GetDetailsAsync(int id)
    {
        var r = await _recipes.GetDetailsAsync(id);
        if (r is null)
            return null;

        var ingredients = r.Ingredients.Select(i => new RecipeIngredientLine(
            i.Ingredient.Name,
            i.Amount,
            (i.Unit ?? i.Ingredient.Unit).Name
        )).ToList();

        var steps = r.Steps
            .OrderBy(s => s.Order)
            .Select(s => new RecipeStepLine(s.Order, s.Content))
            .ToList();

        var commentsDto = r.Comments
            .Where(c => c.ReplyToId == null) // Na razie bierzemy tylko komentarze główne (bez parentów)
            .Select(c => MapCommentWithReplies(c, r.Comments))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        // Średnia ocena sprzed 30 dni (time-travel po temporal Reviews) — do pokazania trendu.
        var asOf30 = DateTime.UtcNow.AddDays(-30);
        var pastRating = (await _db.GetRecipeAvgRatingAsOf(id, asOf30).FirstOrDefaultAsync())?.AverageRating;

        // Wartości odżywcze liczy funkcja SQL fn_RecipeNutrition (przez fn_IngredientAmountInGrams).
        // Calories == null oznacza, że przepisu nie da się policzyć (suma ukryta).
        var nut = await _db.GetRecipeNutrition(id).FirstOrDefaultAsync();
        var nutrition = nut?.Calories is null ? null : new RecipeNutritionSummary(
            Math.Round(nut.Calories.Value), Math.Round(nut.Protein!.Value, 1), Math.Round(nut.Fat!.Value, 1),
            Math.Round(nut.Carbs!.Value, 1), Math.Round(nut.Fiber!.Value, 1), Math.Round(nut.Sugar!.Value, 1), r.Servings);

        return new RecipeDetailsDto(
            r.Id,
            r.Name,
            r.Description,
            r.PrepTimeMinutes,
            r.CookTimeMinutes,
            r.Servings,
            r.DifficultyLevel.Name,
            AuthorName(r.User),
            r.UserId,
            r.CreatedAt,
            r.Reviews.Count > 0 ? r.Reviews.Average(x => x.Rating) : null,
            r.Reviews.Count,
            pastRating,
            r.Images.OrderBy(i => i.Order).Select(i => ImageUrl(i.ImageId)).ToList(),
            ingredients,
            steps,
            r.Categories.Select(c => c.Category.Name).ToList(),
            r.Tags.Select(t => t.Tag.Name).ToList(),
            commentsDto,
            nutrition
        );
    }

    private CommentDto MapCommentWithReplies(Comment comment, ICollection<Comment> allComments)
    {
        var replies = allComments
            .Where(c => c.ReplyToId == comment.Id)
            .Select(c => MapCommentWithReplies(c, allComments))
            .OrderBy(c => c.CreatedAt)
            .ToList();

        var reactions = comment.Reactions
            .GroupBy(cr => new { cr.Reaction.Id, cr.Reaction.Name, cr.Reaction.ImageId })
            .Select(g => new CommentReactionDto(g.Key.Id, g.Key.Name, ImageUrl(g.Key.ImageId), g.Count(), false))
            .ToList();

        return new CommentDto(
            comment.Id,
            comment.UserId,
            AuthorName(comment.User),
            comment.Content,
            comment.CreatedAt,
            comment.ReplyToId,
            replies,
            reactions
        );
    }

    public async Task PopulateLookupsAsync(RecipeFormViewModel vm)
    {
        vm.DifficultyLevels = (await _difficultyLevels.GetAllAsync())
            .OrderBy(d => d.Id).Select(d => new LookupItem(d.Id, d.Name)).ToList();
        vm.AllIngredients = (await _ingredients.GetAllAsync())
            .OrderBy(i => i.Name).Select(i => new LookupItem(i.Id, i.Name)).ToList();
        vm.AllUnits = (await _units.GetAllAsync())
            .OrderBy(u => u.Id).Select(u => new LookupItem(u.Id, u.Name)).ToList();
        vm.AllCategories = (await _categories.GetAllAsync())
            .OrderBy(c => c.Name).Select(c => new LookupItem(c.Id, c.Name)).ToList();
        vm.AllTags = (await _tags.GetAllAsync())
            .OrderBy(t => t.Name).Select(t => new LookupItem(t.Id, t.Name)).ToList();
    }

    public async Task<RecipeFormViewModel?> GetForEditAsync(int id)
    {
        var r = await _recipes.GetForEditAsync(id);
        if (r is null)
            return null;

        var vm = new RecipeFormViewModel
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            PrepTimeMinutes = r.PrepTimeMinutes,
            CookTimeMinutes = r.CookTimeMinutes,
            Servings = r.Servings,
            DifficultyLevelId = r.DifficultyLevelId,
            Ingredients = r.Ingredients.Select(i => new RecipeIngredientInput
            {
                IngredientName = i.Ingredient.Name,
                Amount = i.Amount,
                UnitId = i.UnitId
            }).ToList(),
            Steps = r.Steps.OrderBy(s => s.Order).Select(s => new RecipeStepInput
            {
                Content = s.Content
            }).ToList(),
            SelectedCategoryIds = r.Categories.Select(c => c.CategoryId).ToList(),
            SelectedTagIds = r.Tags.Select(t => t.TagId).ToList(),
            ExistingImages = r.Images.OrderBy(i => i.Order)
                .Select(i => new ExistingImage(i.ImageId, ImageUrl(i.ImageId))).ToList()
        };

        await PopulateLookupsAsync(vm);
        return vm;
    }

    public async Task<(bool Success, string? Error, int RecipeId)> CreateAsync(RecipeFormViewModel vm, int userId)
    {
        var validation = Validate(vm);
        if (validation is not null)
            return (false, validation, 0);

        var recipe = new Recipe
        {
            Name = vm.Name,
            Description = vm.Description,
            PrepTimeMinutes = vm.PrepTimeMinutes,
            CookTimeMinutes = vm.CookTimeMinutes,
            Servings = vm.Servings,
            DifficultyLevelId = vm.DifficultyLevelId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        ApplySteps(recipe, vm);
        await ApplyIngredientsAsync(recipe, vm);
        ApplyCategoriesAndTags(recipe, vm);

        var imageError = await AddNewImagesAsync(recipe, vm, userId);
        if (imageError is not null)
            return (false, imageError, 0);

        await _recipes.AddAsync(recipe);
        await _recipes.SaveChangesAsync();
        return (true, null, recipe.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(RecipeFormViewModel vm, int userId, bool isModerator)
    {
        var validation = Validate(vm);
        if (validation is not null)
            return (false, validation);

        var recipe = await _recipes.GetForEditAsync(vm.Id);
        if (recipe is null)
            return (false, "Nie znaleziono przepisu.");

        if (recipe.UserId != userId && !isModerator)
            return (false, "Brak uprawnień do edycji tego przepisu.");

        recipe.Name = vm.Name;
        recipe.Description = vm.Description;
        recipe.PrepTimeMinutes = vm.PrepTimeMinutes;
        recipe.CookTimeMinutes = vm.CookTimeMinutes;
        recipe.Servings = vm.Servings;
        recipe.DifficultyLevelId = vm.DifficultyLevelId;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Najprostsza i czytelna strategia: wyczyść kolekcje i zbuduj od nowa.
        recipe.Steps.Clear();
        recipe.Ingredients.Clear();
        recipe.Categories.Clear();
        recipe.Tags.Clear();
        ApplySteps(recipe, vm);
        await ApplyIngredientsAsync(recipe, vm);
        ApplyCategoriesAndTags(recipe, vm);

        RemoveSelectedImages(recipe, vm);

        var imageError = await AddNewImagesAsync(recipe, vm, userId);
        if (imageError is not null)
            return (false, imageError);

        _recipes.Update(recipe);
        await _recipes.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, int userId, bool isModerator)
    {
        var recipe = await _recipes.GetForEditAsync(id);
        if (recipe is null)
            return (false, "Nie znaleziono przepisu.");

        if (recipe.UserId != userId && !isModerator)
            return (false, "Brak uprawnień do usunięcia tego przepisu.");

        // RecipeImage znika kaskadowo z przepisem; same bloby (Image) usuwamy osobno.
        var imageIds = recipe.Images.Select(i => i.ImageId).ToList();

        // Surowy DELETE odpala trigger trg_Recipes_DeleteCascadeComments (usuwa komentarze
        // przepisu, których FK jest NO ACTION); reszta dzieci kaskaduje na poziomie bazy.
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM Recipes WHERE Id = @p0", id);

        foreach (var imageId in imageIds)
            _images.Remove(new Image { Id = imageId });
        await _images.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddCommentAsync(int recipeId, int userId, string content, int? replyToId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (false, "Treść komentarza nie może być pusta.");

        var recipeExists = (await _recipes.GetDetailsAsync(recipeId)) != null;
        if (!recipeExists)
            return (false, "Nie znaleziono przepisu.");

        var comment = new Comment
        {
            RecipeId = recipeId,
            UserId = userId,
            Content = content,
            ReplyToId = replyToId,
            CreatedAt = DateTime.UtcNow
        };

        await _comments.AddAsync(comment);
        await _comments.SaveChangesAsync();

        // Powiadomienia tworzy trigger bazodanowy trg_Comments_AfterInsert_Notify
        // (odpowiedź → autor komentarza-rodzica, komentarz główny → autor przepisu).

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCommentAsync(int commentId, int userId, bool isModerator)
    {
        var comment = await _comments.Query().FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment is null)
            return (false, "Komentarz nie istnieje.");
        if (comment.UserId != userId && !isModerator)
            return (false, "Brak uprawnień.");

        // Trigger trg_Comments_DeleteCascade przechwytuje DELETE i usuwa całe poddrzewo odpowiedzi
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM Comments WHERE Id = @p0", commentId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleCommentReactionAsync(int commentId, int userId, int reactionId)
    {
        var existingReaction = (await _commentReactions.GetAllAsync())
            .FirstOrDefault(cr => cr.CommentId == commentId && cr.UserId == userId);

        if (existingReaction != null && existingReaction.ReactionId == reactionId)
        {
            // Ta sama reakcja — odznacz
            _commentReactions.Remove(existingReaction);
        }
        else if (existingReaction != null)
        {
            // Inna reakcja — usuń starą najpierw, potem dodaj nową (osobne SaveChanges, bo klucz główny ten sam)
            _commentReactions.Remove(existingReaction);
            await _commentReactions.SaveChangesAsync();
            await _commentReactions.AddAsync(new CommentReaction
            {
                CommentId = commentId,
                UserId = userId,
                ReactionId = reactionId
            });
        }
        else
        {
            // Brak reakcji — dodaj nową
            await _commentReactions.AddAsync(new CommentReaction
            {
                CommentId = commentId,
                UserId = userId,
                ReactionId = reactionId
            });
        }

        await _commentReactions.SaveChangesAsync();
        return (true, null);
    }

    private static string? Validate(RecipeFormViewModel vm)
    {
        if (vm.DifficultyLevelId <= 0)
            return "Wybierz poziom trudności.";
        if (!vm.Ingredients.Any(i => !string.IsNullOrWhiteSpace(i.IngredientName)))
            return "Dodaj przynajmniej jeden składnik.";
        if (vm.Steps.Count == 0)
            return "Dodaj przynajmniej jeden krok.";
        return null;
    }

    private static void ApplySteps(Recipe recipe, RecipeFormViewModel vm)
    {
        var order = 1;
        foreach (var step in vm.Steps)
            recipe.Steps.Add(new RecipeStep { Order = order++, Content = step.Content });
    }

    /// <summary>
    /// Buduje listę składników przepisu. Składniki dopasowuje po nazwie (bez rozróżniania
    /// wielkości liter); jeśli nazwa nie istnieje, tworzy nowy składnik (encja użytkownika,
    /// nie słownikowa). Duplikaty tej samej nazwy w formularzu sumuje.
    /// </summary>
    private async Task ApplyIngredientsAsync(Recipe recipe, RecipeFormViewModel vm)
    {
        var defaultUnitId = await GetDefaultUnitIdAsync();

        var groups = vm.Ingredients
            .Where(i => !string.IsNullOrWhiteSpace(i.IngredientName))
            .GroupBy(i => i.IngredientName!.Trim(), StringComparer.OrdinalIgnoreCase);

        var newIngredients = new List<Ingredient>();

        foreach (var group in groups)
        {
            var name = group.Key.Trim();
            var amount = group.Sum(i => i.Amount);
            var unitId = group.First().UnitId;

            var existing = await _ingredients.Query().FirstOrDefaultAsync(i => i.Name == name);

            var line = new RecipeIngredient { Amount = amount, UnitId = unitId };
            if (existing is not null)
            {
                line.IngredientId = existing.Id;
            }
            else
            {
                var newIngredient = new Ingredient { Name = name, UnitId = unitId ?? defaultUnitId };
                line.Ingredient = newIngredient;
                newIngredients.Add(newIngredient);
            }

            recipe.Ingredients.Add(line);
        }

        // Dla nowych składników dociągamy wartości odżywcze (równolegle, graceful — nie wywala zapisu).
        // Wartości zapisują się wraz z grafem przepisu w jednym SaveChanges.
        if (newIngredients.Count > 0)
            await Task.WhenAll(newIngredients.Select(i => _nutrition.PopulateNutritionAsync(i)));
    }

    private async Task<int> GetDefaultUnitIdAsync()
    {
        var units = await _units.GetAllAsync();
        return units.OrderBy(u => u.Id).First().Id;
    }

    private static void ApplyCategoriesAndTags(Recipe recipe, RecipeFormViewModel vm)
    {
        foreach (var categoryId in vm.SelectedCategoryIds.Distinct())
            recipe.Categories.Add(new RecipeCategory { CategoryId = categoryId });

        foreach (var tagId in vm.SelectedTagIds.Distinct())
            recipe.Tags.Add(new RecipeTag { TagId = tagId });
    }

    private void RemoveSelectedImages(Recipe recipe, RecipeFormViewModel vm)
    {
        if (vm.RemoveImageIds.Count == 0)
            return;

        var toRemove = recipe.Images.Where(i => vm.RemoveImageIds.Contains(i.ImageId)).ToList();
        foreach (var ri in toRemove)
        {
            recipe.Images.Remove(ri);
            _images.Remove(new Image { Id = ri.ImageId });
        }
    }

    private async Task<string?> AddNewImagesAsync(Recipe recipe, RecipeFormViewModel vm, int userId)
    {
        if (vm.NewImages is null || vm.NewImages.Count == 0)
            return null;

        var nextOrder = recipe.Images.Count > 0 ? recipe.Images.Max(i => i.Order) + 1 : 1;
        foreach (var file in vm.NewImages)
        {
            var (data, contentType, error) = await _imageService.ReadAsync(file);
            if (error is not null)
                return error;

            recipe.Images.Add(new RecipeImage
            {
                Order = nextOrder++,
                Image = new Image { Data = data!, ContentType = contentType!, UploadedById = userId }
            });
        }

        return null;
    }
    
    public async Task<(bool Success, string? Error)> AddReviewAsync(int recipeId, int userId, int rating)
    {
        // 1. Walidacja oceny
        if (rating < 1 || rating > 5)
            return (false, "Ocena musi być w przedziale od 1 do 5.");

        // 2. Sprawdzenie, czy użytkownik już ocenił ten przepis
        // Upewnij się, że używasz odpowiedniej nazwy dla DbContext (np. _db lub _context)
        var existingReview = await _db.Reviews
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);

        if (existingReview != null)
        {
            // Aktualizacja istniejącej oceny
            existingReview.Rating = rating;
            existingReview.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            // Dodanie nowej oceny
            var newReview = new Review
            {
                RecipeId = recipeId,
                UserId = userId,
                Rating = rating,
                CreatedAt = DateTime.UtcNow
            };
            await _db.Reviews.AddAsync(newReview);
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    private static string AuthorName(ApplicationUser user) => user.PublicUsername;
}
