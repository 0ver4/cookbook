using CookBook.Dtos;

namespace CookBook.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);

    /// <summary>Tworzy kategorię. Zwraca błąd gdy nazwa jest już zajęta.</summary>
    Task<(bool Success, string? Error)> CreateAsync(CreateCategoryDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(UpdateCategoryDto dto);
    Task<bool> DeleteAsync(int id);
}
