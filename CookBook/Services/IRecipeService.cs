using CookBook.Dtos;
using CookBook.ViewModels;

namespace CookBook.Services;

public interface IRecipeService
{
    Task<IReadOnlyList<RecipeListItemDto>> GetListAsync();
    Task<RecipeDetailsDto?> GetDetailsAsync(int id);

    /// <summary>Uzupełnia model formularza danymi słownikowymi (trudność, składniki, jednostki, kategorie, tagi).</summary>
    Task PopulateLookupsAsync(RecipeFormViewModel vm);

    /// <summary>Wczytuje istniejący przepis do modelu formularza (edycja).</summary>
    Task<RecipeFormViewModel?> GetForEditAsync(int id);

    Task<(bool Success, string? Error, int RecipeId)> CreateAsync(RecipeFormViewModel vm, int userId);
    Task<(bool Success, string? Error)> UpdateAsync(RecipeFormViewModel vm, int userId, bool isModerator);
    Task<(bool Success, string? Error)> DeleteAsync(int id, int userId, bool isModerator);
    
    Task<(bool Success, string? Error)> AddCommentAsync(int recipeId, int userId, string content, int? replyToId = null);
    Task<(bool Success, string? Error)> DeleteCommentAsync(int commentId, int userId, bool isModerator);
    Task<(bool Success, string? Error)> ToggleCommentReactionAsync(int commentId, int userId, int reactionId);
}
