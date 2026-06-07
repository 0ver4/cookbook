using CookBook.Dtos;
using CookBook.Models;
using CookBook.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
    {
        return await _repository.Query()
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name))
            .ToListAsync();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);
        return category is null ? null : new CategoryDto(category.Id, category.Name);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(CreateCategoryDto dto)
    {
        if (await _repository.NameExistsAsync(dto.Name))
            return (false, "Kategoria o tej nazwie już istnieje.");

        await _repository.AddAsync(new Category { Name = dto.Name });
        await _repository.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(UpdateCategoryDto dto)
    {
        var category = await _repository.GetByIdAsync(dto.Id);
        if (category is null)
            return (false, "Nie znaleziono kategorii.");

        if (await _repository.NameExistsAsync(dto.Name, dto.Id))
            return (false, "Kategoria o tej nazwie już istnieje.");

        category.Name = dto.Name;
        _repository.Update(category);
        await _repository.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category is null)
            return false;

        _repository.Remove(category);
        await _repository.SaveChangesAsync();
        return true;
    }
}
