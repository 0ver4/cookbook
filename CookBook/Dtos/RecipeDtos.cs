using CookBook.ViewModels;

namespace CookBook.Dtos;

/// <summary>Parametry filtrowania/sortowania/stronicowania listy przepisów (przekazywane przez query string).</summary>
public record RecipeQuery(
    string? Search = null,
    int? CategoryId = null,
    int? DifficultyId = null,
    string Sort = "newest",
    int Page = 1,
    int PageSize = 9);

/// <summary>Skrócony przepis na liście/kartach.</summary>
public record RecipeListItemDto(
    int Id,
    string Name,
    string? ImageUrl,
    string DifficultyName,
    string AuthorName,
    double? AverageRating,
    int ReviewCount,
    IReadOnlyList<string> Categories);

/// <summary>Dane potrzebne do wyrenderowania listy z filtrami i paginacją.</summary>
public record RecipeListViewModel(
    IReadOnlyList<RecipeListItemDto> Recipes,
    IReadOnlyList<LookupItem> Categories,
    IReadOnlyList<LookupItem> Difficulties,
    RecipeQuery Query,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>Pojedyncza pozycja składnika w widoku szczegółów.</summary>
public record RecipeIngredientLine(string IngredientName, double Amount, string UnitName);

/// <summary>Pojedynczy krok w widoku szczegółów.</summary>
public record RecipeStepLine(int Order, string Content);

/// <summary>
/// Zsumowane wartości odżywcze całego przepisu (kalorie w kcal, reszta w gramach).
/// Wyliczane tylko, gdy WSZYSTKIE składniki da się przeliczyć na gramy i mają dane odżywcze.
/// </summary>
public record RecipeNutritionSummary(
    double Calories, double Protein, double Fat, double Carbs, double Fiber, double Sugar, int? Servings);

public record CommentReactionDto(int ReactionId, string ReactionName, string ReactionImageUrl, int Count, bool UserReacted);

public record CommentDto(
    int Id,
    int UserId,
    string AuthorName,
    string Content,
    DateTime CreatedAt,
    int? ReplyToId,
    List<CommentDto>? Replies,
    IReadOnlyList<CommentReactionDto> Reactions);

/// <summary>Pełne dane przepisu do widoku szczegółów.</summary>
public record RecipeDetailsDto(
    int Id,
    string Name,
    string? Description,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    int? Servings,
    string DifficultyName,
    string AuthorName,
    int OwnerId,
    DateTime CreatedAt,
    double? AverageRating,
    int ReviewCount,
    double? AverageRating30DaysAgo,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<RecipeIngredientLine> Ingredients,
    IReadOnlyList<RecipeStepLine> Steps,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    IReadOnlyList<CommentDto> Comments,
    RecipeNutritionSummary? Nutrition);
