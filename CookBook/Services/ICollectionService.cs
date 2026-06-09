using CookBook.Dtos;

namespace CookBook.Services;

public interface ICollectionService
{
    Task<IReadOnlyList<CollectionSummaryDto>> GetForUserAsync(int userId);
    Task<CollectionDetailsDto?> GetDetailsAsync(int id, int userId);
    Task<(bool Success, string? Error, int CollectionId)> CreateAsync(int userId, string name);
    Task<(bool Success, string? Error)> RenameAsync(int id, int userId, string name);
    Task<(bool Success, string? Error)> DeleteAsync(int id, int userId);
    Task<(bool Success, string? Error)> AddRecipeAsync(int collectionId, int userId, int recipeId);
    Task<(bool Success, string? Error)> RemoveRecipeAsync(int collectionId, int userId, int recipeId);
}
