using CookBook.Models;
using CookBook.Repositories;
using CookBook.Services;
using MockQueryable.Moq;
using Moq;

namespace CookBook.Tests;

public class ShoppingListServiceTests
{
    private readonly Mock<IShoppingListRepository> _lists = new();
    private readonly Mock<IRecipeRepository> _recipes = new();
    private readonly Mock<IRepository<Ingredient>> _ingredients = new();
    private readonly Mock<IRepository<Unit>> _units = new();

    private ShoppingListService CreateService() =>
        new(_lists.Object, _recipes.Object, _ingredients.Object, _units.Object);

    // Helper: składnik o danym id/nazwie z domyślną jednostką
    private static Ingredient Ingredient(int id, string name, int unitId = 1) =>
        new() { Id = id, Name = name, UnitId = unitId, Unit = new Unit { Id = unitId, Name = "g" } };

    // Helper: pozycja listy z dociągniętymi nawigacjami (do mapowania DTO)
    private static ShoppingListItem Item(int ingredientId, string ingredientName, int unitId, string unitName, double amount, bool isChecked = false)
        => new()
        {
            IngredientId = ingredientId,
            Ingredient = new Ingredient { Id = ingredientId, Name = ingredientName },
            UnitId = unitId,
            Unit = new Unit { Id = unitId, Name = unitName },
            Amount = amount,
            IsChecked = isChecked
        };

    // -----------------------------------------------------------------------
    // GetForUserAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetForUserAsync_MapsSummaryAndCountsCheckedItems()
    {
        // Arrange
        var list = new ShoppingList
        {
            Id = 1,
            Name = "Tygodniowe",
            CreatedAt = new DateTime(2026, 1, 1),
            Items =
            {
                Item(1, "Mąka", 1, "g", 500, isChecked: true),
                Item(2, "Cukier", 1, "g", 200, isChecked: false),
                Item(3, "Jajka", 2, "szt", 6, isChecked: true),
            }
        };
        _lists.Setup(r => r.GetForUserAsync(7)).ReturnsAsync(new[] { list });
        var svc = CreateService();

        // Act
        var result = await svc.GetForUserAsync(7);

        // Assert
        var dto = Assert.Single(result);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Tygodniowe", dto.Name);
        Assert.Equal(3, dto.ItemCount);
        Assert.Equal(2, dto.CheckedCount); // dwie odhaczone
    }

    [Fact]
    public async Task GetForUserAsync_WhenNoLists_ReturnsEmpty()
    {
        _lists.Setup(r => r.GetForUserAsync(It.IsAny<int>())).ReturnsAsync(Array.Empty<ShoppingList>());
        var svc = CreateService();

        var result = await svc.GetForUserAsync(1);

        Assert.Empty(result);
    }

    // -----------------------------------------------------------------------
    // GetDetailsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetDetailsAsync_WhenListMissing_ReturnsNull()
    {
        _lists.Setup(r => r.GetWithItemsAsync(99)).ReturnsAsync((ShoppingList?)null);
        var svc = CreateService();

        var result = await svc.GetDetailsAsync(99, 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenWrongUser_ReturnsNull()
    {
        var list = new ShoppingList { Id = 1, UserId = 2, Name = "Cudza" };
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        var svc = CreateService();

        var result = await svc.GetDetailsAsync(1, userId: 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDetailsAsync_OrdersItemsByIngredientThenUnit()
    {
        var list = new ShoppingList
        {
            Id = 1,
            UserId = 1,
            Name = "Lista",
            Items =
            {
                Item(2, "Cukier", 1, "g", 100),
                Item(1, "Banan",  1, "szt", 3),
                Item(3, "Mleko",  1, "ml", 500),
            }
        };
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        var svc = CreateService();

        var result = await svc.GetDetailsAsync(1, 1);

        Assert.NotNull(result);
        // alfabetycznie po nazwie składnika: Banan, Cukier, Mleko
        Assert.Equal(new[] { "Banan", "Cukier", "Mleko" }, result!.Items.Select(i => i.IngredientName));
    }

    [Fact]
    public async Task GetDetailsAsync_MapsItemFields()
    {
        var list = new ShoppingList
        {
            Id = 5, UserId = 1, Name = "Lista", CreatedAt = new DateTime(2026, 2, 2),
            Items = { Item(8, "Masło", 3, "kg", 0.25, isChecked: true) }
        };
        _lists.Setup(r => r.GetWithItemsAsync(5)).ReturnsAsync(list);
        var svc = CreateService();

        var result = await svc.GetDetailsAsync(5, 1);

        var item = Assert.Single(result!.Items);
        Assert.Equal(8, item.IngredientId);
        Assert.Equal(3, item.UnitId);
        Assert.Equal("Masło", item.IngredientName);
        Assert.Equal(0.25, item.Amount);
        Assert.Equal("kg", item.UnitName);
        Assert.True(item.IsChecked);
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_WhenNameBlank_Fails(string? name)
    {
        var svc = CreateService();

        var (success, error, listId) = await svc.CreateAsync(1, name!);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Equal(0, listId);
        _lists.Verify(r => r.AddAsync(It.IsAny<ShoppingList>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_AddsTrimmedListAndSaves()
    {
        ShoppingList? captured = null;
        _lists.Setup(r => r.AddAsync(It.IsAny<ShoppingList>()))
            .Callback<ShoppingList>(l => { captured = l; l.Id = 11; })
            .Returns(Task.CompletedTask);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, error, listId) = await svc.CreateAsync(7, "  Zakupy  ");

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(11, listId);
        Assert.Equal("Zakupy", captured!.Name); // przycięte
        Assert.Equal(7, captured.UserId);
        _lists.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenMissing_Fails()
    {
        _lists.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ShoppingList?)null);
        var svc = CreateService();

        var (success, error) = await svc.DeleteAsync(1, 1);

        Assert.False(success);
        Assert.NotNull(error);
        _lists.Verify(r => r.Remove(It.IsAny<ShoppingList>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenWrongUser_Fails()
    {
        _lists.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ShoppingList { Id = 1, UserId = 2 });
        var svc = CreateService();

        var (success, _) = await svc.DeleteAsync(1, userId: 1);

        Assert.False(success);
        _lists.Verify(r => r.Remove(It.IsAny<ShoppingList>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenOwned_RemovesAndSaves()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        _lists.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(list);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, error) = await svc.DeleteAsync(1, 1);

        Assert.True(success);
        Assert.Null(error);
        _lists.Verify(r => r.Remove(list), Times.Once);
        _lists.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // -----------------------------------------------------------------------
    // AddItemAsync (zawiera logikę MergeItem + ResolveIngredient)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddItemAsync_WhenNameBlank_Fails()
    {
        var svc = CreateService();

        var (success, error) = await svc.AddItemAsync(1, 1, "  ", 100, null);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task AddItemAsync_WhenAmountNotPositive_Fails(double amount)
    {
        var svc = CreateService();

        var (success, error) = await svc.AddItemAsync(1, 1, "Mąka", amount, null);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task AddItemAsync_WhenWrongUser_Fails()
    {
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(new ShoppingList { Id = 1, UserId = 2 });
        var svc = CreateService();

        var (success, _) = await svc.AddItemAsync(1, userId: 1, "Mąka", 100, null);

        Assert.False(success);
    }

    [Fact]
    public async Task AddItemAsync_WhenIngredientExists_AddsNewItemWithGivenUnit()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        // Składnik "Mąka" już istnieje -> ResolveIngredientAsync zwróci go przez Query()
        _ingredients.Setup(r => r.Query())
            .Returns(new[] { Ingredient(3, "Mąka", unitId: 1) }.AsQueryable().BuildMock());
        var svc = CreateService();

        var (success, _) = await svc.AddItemAsync(1, 1, "Mąka", 250, unitId: 5);

        Assert.True(success);
        var item = Assert.Single(list.Items);
        Assert.Equal(3, item.IngredientId);
        Assert.Equal(5, item.UnitId); // podana jednostka wygrywa z domyślną składnika
        Assert.Equal(250, item.Amount);
        _ingredients.Verify(r => r.AddAsync(It.IsAny<Ingredient>()), Times.Never); // nie tworzymy nowego
    }

    [Fact]
    public async Task AddItemAsync_WhenSameIngredientAndUnit_MergesAmount()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        list.Items.Add(new ShoppingListItem { IngredientId = 3, UnitId = 1, Amount = 100 });
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _ingredients.Setup(r => r.Query())
            .Returns(new[] { Ingredient(3, "Mąka", unitId: 1) }.AsQueryable().BuildMock());
        var svc = CreateService();

        // dodaj 250 tej samej mąki w tej samej jednostce (1) -> ma się zsumować
        var (success, _) = await svc.AddItemAsync(1, 1, "Mąka", 250, unitId: 1);

        Assert.True(success);
        var item = Assert.Single(list.Items);
        Assert.Equal(350, item.Amount); // 100 + 250
    }

    [Fact]
    public async Task AddItemAsync_WhenIngredientNew_CreatesItWithDefaultUnit()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        // Składnik nie istnieje -> Query pusty
        _ingredients.Setup(r => r.Query()).Returns(Array.Empty<Ingredient>().AsQueryable().BuildMock());
        _ingredients.Setup(r => r.AddAsync(It.IsAny<Ingredient>())).Returns(Task.CompletedTask);
        // Domyślna jednostka = najniższe Id
        _units.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[] { new Unit { Id = 2, Name = "g" }, new Unit { Id = 9, Name = "kg" } });
        var svc = CreateService();

        // nie podajemy unitId -> użyje domyślnej jednostki świeżo utworzonego składnika
        var (success, _) = await svc.AddItemAsync(1, 1, "Curry", 30, unitId: null);

        Assert.True(success);
        _ingredients.Verify(r => r.AddAsync(It.Is<Ingredient>(i => i.Name == "Curry" && i.UnitId == 2)), Times.Once);
        var item = Assert.Single(list.Items);
        Assert.Equal(2, item.UnitId);
        Assert.Equal(30, item.Amount);
    }

    // -----------------------------------------------------------------------
    // RemoveItemAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RemoveItemAsync_WhenWrongUser_Fails()
    {
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(new ShoppingList { Id = 1, UserId = 2 });
        var svc = CreateService();

        var (success, _) = await svc.RemoveItemAsync(1, userId: 1, ingredientId: 1, unitId: 1);

        Assert.False(success);
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesMatchingItem()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        list.Items.Add(new ShoppingListItem { IngredientId = 3, UnitId = 1, Amount = 100 });
        list.Items.Add(new ShoppingListItem { IngredientId = 4, UnitId = 1, Amount = 50 });
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, _) = await svc.RemoveItemAsync(1, 1, ingredientId: 3, unitId: 1);

        Assert.True(success);
        var remaining = Assert.Single(list.Items);
        Assert.Equal(4, remaining.IngredientId);
        _lists.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_WhenItemMissing_SucceedsWithoutSaving()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        var svc = CreateService();

        var (success, _) = await svc.RemoveItemAsync(1, 1, ingredientId: 99, unitId: 1);

        Assert.True(success);
        _lists.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // -----------------------------------------------------------------------
    // ToggleItemAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ToggleItemAsync_FlipsIsChecked()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        var item = new ShoppingListItem { IngredientId = 3, UnitId = 1, IsChecked = false };
        list.Items.Add(item);
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        await svc.ToggleItemAsync(1, 1, 3, 1);
        Assert.True(item.IsChecked);   // false -> true

        await svc.ToggleItemAsync(1, 1, 3, 1);
        Assert.False(item.IsChecked);  // true -> false
    }

    [Fact]
    public async Task ToggleItemAsync_WhenWrongUser_Fails()
    {
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(new ShoppingList { Id = 1, UserId = 2 });
        var svc = CreateService();

        var (success, _) = await svc.ToggleItemAsync(1, userId: 1, 3, 1);

        Assert.False(success);
    }

    // -----------------------------------------------------------------------
    // GenerateFromRecipeAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateFromRecipeAsync_WhenWrongUser_Fails()
    {
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(new ShoppingList { Id = 1, UserId = 2 });
        var svc = CreateService();

        var (success, _) = await svc.GenerateFromRecipeAsync(1, userId: 1, recipeId: 5);

        Assert.False(success);
    }

    [Fact]
    public async Task GenerateFromRecipeAsync_WhenRecipeMissing_Fails()
    {
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(new ShoppingList { Id = 1, UserId = 1 });
        _recipes.Setup(r => r.GetDetailsAsync(5)).ReturnsAsync((Recipe?)null);
        var svc = CreateService();

        var (success, error) = await svc.GenerateFromRecipeAsync(1, 1, 5);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task GenerateFromRecipeAsync_AddsRecipeIngredientsToList()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var recipe = new Recipe
        {
            Id = 5, Name = "Naleśniki",
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 3, Amount = 200, UnitId = 1 },
                new RecipeIngredient { IngredientId = 4, Amount = 2,   UnitId = null }, // użyje domyślnej jednostki składnika
            }
        };
        _recipes.Setup(r => r.GetDetailsAsync(5)).ReturnsAsync(recipe);
        _ingredients.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(Ingredient(3, "Mąka", unitId: 1));
        _ingredients.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(Ingredient(4, "Jajka", unitId: 7));
        var svc = CreateService();

        var (success, _) = await svc.GenerateFromRecipeAsync(1, 1, 5);

        Assert.True(success);
        Assert.Equal(2, list.Items.Count);
        Assert.Contains(list.Items, i => i.IngredientId == 3 && i.UnitId == 1 && i.Amount == 200);
        Assert.Contains(list.Items, i => i.IngredientId == 4 && i.UnitId == 7 && i.Amount == 2); // fallback do UnitId składnika
    }

    [Fact]
    public async Task GenerateFromRecipeAsync_MergesWithExistingItems()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        list.Items.Add(new ShoppingListItem { IngredientId = 3, UnitId = 1, Amount = 100 });
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var recipe = new Recipe
        {
            Id = 5, Name = "Chleb",
            Ingredients = { new RecipeIngredient { IngredientId = 3, Amount = 400, UnitId = 1 } }
        };
        _recipes.Setup(r => r.GetDetailsAsync(5)).ReturnsAsync(recipe);
        _ingredients.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(Ingredient(3, "Mąka", unitId: 1));
        var svc = CreateService();

        await svc.GenerateFromRecipeAsync(1, 1, 5);

        var item = Assert.Single(list.Items);
        Assert.Equal(500, item.Amount); // 100 (istniejące) + 400 (z przepisu)
    }

    [Fact]
    public async Task GenerateFromRecipeAsync_SkipsIngredientNotFoundInRepo()
    {
        var list = new ShoppingList { Id = 1, UserId = 1 };
        _lists.Setup(r => r.GetWithItemsAsync(1)).ReturnsAsync(list);
        _lists.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var recipe = new Recipe
        {
            Id = 5, Name = "X",
            Ingredients = { new RecipeIngredient { IngredientId = 999, Amount = 10, UnitId = 1 } }
        };
        _recipes.Setup(r => r.GetDetailsAsync(5)).ReturnsAsync(recipe);
        _ingredients.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Ingredient?)null);
        var svc = CreateService();

        var (success, _) = await svc.GenerateFromRecipeAsync(1, 1, 5);

        Assert.True(success);
        Assert.Empty(list.Items); // brakujący składnik pominięty
    }
}
