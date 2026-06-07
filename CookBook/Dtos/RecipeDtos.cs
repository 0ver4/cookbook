namespace CookBook.Dtos;

/// <summary>Skrócony przepis na liście/kartach.</summary>
public record RecipeListItemDto(
    int Id,
    string Name,
    string? ImageUrl,
    string DifficultyName,
    string AuthorName,
    double? AverageRating,
    int ReviewCount);

/// <summary>Pojedyncza pozycja składnika w widoku szczegółów.</summary>
public record RecipeIngredientLine(string IngredientName, double Amount, string UnitName);

/// <summary>Pojedynczy krok w widoku szczegółów.</summary>
public record RecipeStepLine(int Order, string Content);

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
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<RecipeIngredientLine> Ingredients,
    IReadOnlyList<RecipeStepLine> Steps,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags);
