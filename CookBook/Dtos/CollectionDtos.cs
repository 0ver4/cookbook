namespace CookBook.Dtos;

public record CollectionSummaryDto(int Id, string Name, DateTime CreatedAt, int RecipeCount);

public record CollectionRecipeDto(int RecipeId, string Name, string? ImageUrl, string DifficultyName);

public record CollectionDetailsDto(
    int Id,
    string Name,
    DateTime CreatedAt,
    IReadOnlyList<CollectionRecipeDto> Recipes);
