using CookBook.Dtos;
using CookBook.Models;
using CookBook.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Services;

public class ShoppingListService : IShoppingListService
{
    private readonly IShoppingListRepository _lists;
    private readonly IRecipeRepository _recipes;
    private readonly IRepository<Ingredient> _ingredients;
    private readonly IRepository<Unit> _units;

    public ShoppingListService(
        IShoppingListRepository lists,
        IRecipeRepository recipes,
        IRepository<Ingredient> ingredients,
        IRepository<Unit> units)
    {
        _lists = lists;
        _recipes = recipes;
        _ingredients = ingredients;
        _units = units;
    }

    public async Task<IReadOnlyList<ShoppingListSummaryDto>> GetForUserAsync(int userId)
    {
        var lists = await _lists.GetForUserAsync(userId);
        return lists.Select(s => new ShoppingListSummaryDto(
            s.Id,
            s.Name,
            s.CreatedAt,
            s.Items.Count,
            s.Items.Count(i => i.IsChecked)
        )).ToList();
    }

    public async Task<ShoppingListDetailsDto?> GetDetailsAsync(int id, int userId)
    {
        var list = await _lists.GetWithItemsAsync(id);
        if (list is null || list.UserId != userId)
            return null;

        var items = list.Items
            .OrderBy(i => i.Ingredient.Name).ThenBy(i => i.Unit.Name)
            .Select(i => new ShoppingListItemDto(
                i.IngredientId,
                i.UnitId,
                i.Ingredient.Name,
                i.Amount,
                i.Unit.Name,
                i.IsChecked))
            .ToList();

        return new ShoppingListDetailsDto(list.Id, list.Name, list.CreatedAt, items);
    }

    public async Task<(bool Success, string? Error, int ListId)> CreateAsync(int userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Podaj nazwę listy.", 0);

        var list = new ShoppingList { UserId = userId, Name = name.Trim(), CreatedAt = DateTime.UtcNow };
        await _lists.AddAsync(list);
        await _lists.SaveChangesAsync();
        return (true, null, list.Id);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, int userId)
    {
        var list = await _lists.GetByIdAsync(id);
        if (list is null || list.UserId != userId)
            return (false, "Nie znaleziono listy.");

        _lists.Remove(list);
        await _lists.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddItemAsync(int listId, int userId, string ingredientName, double amount, int? unitId)
    {
        if (string.IsNullOrWhiteSpace(ingredientName))
            return (false, "Podaj nazwę składnika.");
        if (amount <= 0)
            return (false, "Ilość musi być większa od zera.");

        var list = await _lists.GetWithItemsAsync(listId);
        if (list is null || list.UserId != userId)
            return (false, "Nie znaleziono listy.");

        var ingredient = await ResolveIngredientAsync(ingredientName.Trim());
        MergeItem(list, ingredient, unitId ?? ingredient.UnitId, amount);

        await _lists.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveItemAsync(int listId, int userId, int ingredientId, int unitId)
    {
        var list = await _lists.GetWithItemsAsync(listId);
        if (list is null || list.UserId != userId)
            return (false, "Nie znaleziono listy.");

        var item = list.Items.FirstOrDefault(i => i.IngredientId == ingredientId && i.UnitId == unitId);
        if (item is not null)
        {
            list.Items.Remove(item);
            await _lists.SaveChangesAsync();
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleItemAsync(int listId, int userId, int ingredientId, int unitId)
    {
        var list = await _lists.GetWithItemsAsync(listId);
        if (list is null || list.UserId != userId)
            return (false, "Nie znaleziono listy.");

        var item = list.Items.FirstOrDefault(i => i.IngredientId == ingredientId && i.UnitId == unitId);
        if (item is not null)
        {
            item.IsChecked = !item.IsChecked;
            await _lists.SaveChangesAsync();
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> GenerateFromRecipeAsync(int listId, int userId, int recipeId)
    {
        var list = await _lists.GetWithItemsAsync(listId);
        if (list is null || list.UserId != userId)
            return (false, "Nie znaleziono listy.");

        var recipe = await _recipes.GetDetailsAsync(recipeId);
        if (recipe is null)
            return (false, "Nie znaleziono przepisu.");

        foreach (var ri in recipe.Ingredients)
        {
            var ingredient = await _ingredients.GetByIdAsync(ri.IngredientId);
            if (ingredient is not null)
                MergeItem(list, ingredient, ri.UnitId ?? ingredient.UnitId, ri.Amount);
        }

        await _lists.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Dodaje ilość do pozycji o tym samym składniku i jednostce albo tworzy nową.</summary>
    private static void MergeItem(ShoppingList list, Ingredient ingredient, int unitId, double amount)
    {
        var existing = list.Items.FirstOrDefault(i => i.IngredientId == ingredient.Id && i.UnitId == unitId);
        if (existing is not null)
        {
            existing.Amount += amount;
        }
        else
        {
            list.Items.Add(new ShoppingListItem
            {
                IngredientId = ingredient.Id,
                Ingredient = ingredient,
                UnitId = unitId,
                Amount = amount
            });
        }
    }

    /// <summary>Znajduje składnik po nazwie (bez wielkości liter) lub tworzy nowy z domyślną jednostką.</summary>
    private async Task<Ingredient> ResolveIngredientAsync(string name)
    {
        var existing = await _ingredients.Query().FirstOrDefaultAsync(i => i.Name == name);
        if (existing is not null)
            return existing;

        var defaultUnit = (await _units.GetAllAsync()).OrderBy(u => u.Id).First();
        var ingredient = new Ingredient { Name = name, UnitId = defaultUnit.Id };
        await _ingredients.AddAsync(ingredient);
        return ingredient;
    }
}
