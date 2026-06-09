using CookBook.Dtos;
using CookBook.Models;
using CookBook.Repositories;
using CookBook.Services;
using CookBook.ViewModels;
using Moq;

namespace CookBook.Tests;

public class MealPlanServiceTests
{
    private readonly Mock<IMealPlanRepository> _mealPlan = new();
    private readonly Mock<IRecipeService> _recipeService = new();

    private MealPlanService CreateService() => new(_mealPlan.Object, _recipeService.Object);

    // Minimalny RecipeDetailsDto (potrzebne tylko "nie-null") — wypełniamy wymagane pola domyślnie.
    private static RecipeDetailsDto FakeDetails(int id = 1) => new(
        id, "Przepis", null, null, null, null, "Łatwy", "autor", OwnerId: 1, DateTime.UtcNow,
        AverageRating: null, ReviewCount: 0,
        ImageUrls: Array.Empty<string>(),
        Ingredients: Array.Empty<RecipeIngredientLine>(),
        Steps: Array.Empty<RecipeStepLine>(),
        Categories: Array.Empty<string>(),
        Tags: Array.Empty<string>(),
        Comments: Array.Empty<CommentDto>(),
        Nutrition: null);

    private static RecipeListViewModel FakeList(params (int Id, string Name)[] recipes) => new(
        recipes.Select(r => new RecipeListItemDto(
            r.Id, r.Name, null, "Łatwy", "autor", null, 0, Array.Empty<string>())).ToList(),
        Categories: Array.Empty<LookupItem>(),
        Difficulties: Array.Empty<LookupItem>(),
        Query: new RecipeQuery());

    // -----------------------------------------------------------------------
    // AddAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddAsync_WhenRecipeMissing_Fails()
    {
        _recipeService.Setup(s => s.GetDetailsAsync(5)).ReturnsAsync((RecipeDetailsDto?)null);
        var svc = CreateService();

        var (success, error) = await svc.AddAsync(1, new DateTime(2026, 6, 10), MealType.Lunch, 5);

        Assert.False(success);
        Assert.NotNull(error);
        _mealPlan.Verify(r => r.AddAsync(It.IsAny<MealPlanItem>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenValid_AddsItemWithDateOnlyAndSaves()
    {
        _recipeService.Setup(s => s.GetDetailsAsync(5)).ReturnsAsync(FakeDetails(5));
        MealPlanItem? captured = null;
        _mealPlan.Setup(r => r.AddAsync(It.IsAny<MealPlanItem>()))
            .Callback<MealPlanItem>(m => captured = m).Returns(Task.CompletedTask);
        _mealPlan.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var date = new DateTime(2026, 6, 10, 14, 30, 0); // z godziną
        var (success, _) = await svc.AddAsync(userId: 3, date, MealType.Dinner, recipeId: 5);

        Assert.True(success);
        Assert.NotNull(captured);
        Assert.Equal(3, captured!.UserId);
        Assert.Equal(5, captured.RecipeId);
        Assert.Equal(MealType.Dinner, captured.MealType);
        Assert.Equal(date.Date, captured.Date); // godzina obcięta
        _mealPlan.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // -----------------------------------------------------------------------
    // RemoveAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RemoveAsync_WhenMissing_Fails()
    {
        _mealPlan.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((MealPlanItem?)null);
        var svc = CreateService();

        var (success, error) = await svc.RemoveAsync(1, 1);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task RemoveAsync_WhenWrongUser_Fails()
    {
        _mealPlan.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new MealPlanItem { Id = 1, UserId = 2 });
        var svc = CreateService();

        var (success, error) = await svc.RemoveAsync(userId: 1, itemId: 1);

        Assert.False(success);
        Assert.NotNull(error);
        _mealPlan.Verify(r => r.Remove(It.IsAny<MealPlanItem>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WhenOwned_RemovesAndSaves()
    {
        var item = new MealPlanItem { Id = 1, UserId = 1 };
        _mealPlan.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _mealPlan.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, _) = await svc.RemoveAsync(1, 1);

        Assert.True(success);
        _mealPlan.Verify(r => r.Remove(item), Times.Once);
        _mealPlan.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // -----------------------------------------------------------------------
    // GetWeekAsync — logika tygodnia (poniedziałek-start, 7 dni, sloty)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetWeekAsync_WeekStartsOnMondayOfGivenDate()
    {
        _mealPlan.Setup(r => r.GetForUserInRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<MealPlanItem>());
        _recipeService.Setup(s => s.GetListAsync(It.IsAny<RecipeQuery?>())).ReturnsAsync(FakeList());
        var svc = CreateService();

        // 10 czerwca 2026 to środa -> poniedziałek tego tygodnia = 8 czerwca
        var vm = await svc.GetWeekAsync(1, new DateTime(2026, 6, 10));

        Assert.Equal(new DateTime(2026, 6, 8), vm.WeekStart);
        Assert.Equal(DayOfWeek.Monday, vm.WeekStart.DayOfWeek);
        Assert.Equal(7, vm.Days.Count);
        Assert.Equal(new DateTime(2026, 6, 8), vm.Days.First().Date);
        Assert.Equal(new DateTime(2026, 6, 14), vm.Days.Last().Date); // niedziela
    }

    [Fact]
    public async Task GetWeekAsync_WhenDateIsSunday_StaysInSameWeek()
    {
        _mealPlan.Setup(r => r.GetForUserInRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<MealPlanItem>());
        _recipeService.Setup(s => s.GetListAsync(It.IsAny<RecipeQuery?>())).ReturnsAsync(FakeList());
        var svc = CreateService();

        // 14 czerwca 2026 to niedziela -> poniedziałek = 8 czerwca
        var vm = await svc.GetWeekAsync(1, new DateTime(2026, 6, 14));

        Assert.Equal(new DateTime(2026, 6, 8), vm.WeekStart);
    }

    [Fact]
    public async Task GetWeekAsync_PlacesEntryInCorrectDayAndSlot()
    {
        var monday = new DateTime(2026, 6, 8);
        var item = new MealPlanItem
        {
            Id = 99,
            RecipeId = 5,
            Recipe = new Recipe { Id = 5, Name = "Owsianka" },
            Date = monday,
            MealType = MealType.Breakfast
        };
        _mealPlan.Setup(r => r.GetForUserInRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new[] { item });
        _recipeService.Setup(s => s.GetListAsync(It.IsAny<RecipeQuery?>())).ReturnsAsync(FakeList((5, "Owsianka")));
        var svc = CreateService();

        var vm = await svc.GetWeekAsync(1, monday);

        var firstDay = vm.Days.First();
        var breakfast = firstDay.Meals[MealType.Breakfast];
        var entry = Assert.Single(breakfast);
        Assert.Equal(99, entry.ItemId);
        Assert.Equal(5, entry.RecipeId);
        Assert.Equal("Owsianka", entry.RecipeName);
        // inny slot tego samego dnia jest pusty
        Assert.Empty(firstDay.Meals[MealType.Dinner]);
    }

    [Fact]
    public async Task GetWeekAsync_MapsAvailableRecipesForPicker()
    {
        _mealPlan.Setup(r => r.GetForUserInRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<MealPlanItem>());
        _recipeService.Setup(s => s.GetListAsync(It.IsAny<RecipeQuery?>()))
            .ReturnsAsync(FakeList((1, "Bigos"), (2, "Żurek")));
        var svc = CreateService();

        var vm = await svc.GetWeekAsync(1, new DateTime(2026, 6, 10));

        Assert.Equal(2, vm.Recipes.Count);
        Assert.Contains(vm.Recipes, r => r.Id == 1 && r.Name == "Bigos");
        Assert.Contains(vm.Recipes, r => r.Id == 2 && r.Name == "Żurek");
    }
}
