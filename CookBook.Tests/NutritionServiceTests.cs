using CookBook.Models;
using CookBook.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CookBook.Tests;

public class NutritionServiceTests
{
    private readonly Mock<INutritionProvider> _provider = new();

    private NutritionService CreateService() =>
        new(_provider.Object, NullLogger<NutritionService>.Instance);

    private static Ingredient NewIngredient(string name = "Mąka") =>
        new() { Id = 1, Name = name, UnitId = 1 };

    // Pełny, sensowny zestaw wartości na 100 g
    private static NutritionFacts Facts(
        double cal = 364, double protein = 10, double fat = 1, double carbs = 76,
        double fiber = 3, double sugar = 1, double density = 0, double gramsPerPiece = 0) =>
        new(cal, protein, fat, carbs, fiber, sugar, density, gramsPerPiece);

    // -----------------------------------------------------------------------
    // Brak danych / wyjątki — nic nie dopinamy, nie rzucamy
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PopulateNutritionAsync_WhenProviderReturnsNull_AddsNothing()
    {
        _provider.Setup(p => p.FetchAsync("Mąka", It.IsAny<CancellationToken>())).ReturnsAsync((NutritionFacts?)null);
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        Assert.Empty(ingredient.IngredientNutritions);
    }

    [Fact]
    public async Task PopulateNutritionAsync_WhenProviderThrows_DoesNotThrowAndAddsNothing()
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));
        var ingredient = NewIngredient();
        var svc = CreateService();

        // nie powinno rzucić
        await svc.PopulateNutritionAsync(ingredient);

        Assert.Empty(ingredient.IngredientNutritions);
    }

    // -----------------------------------------------------------------------
    // Walidacja sensowności (IsPlausible)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1501, 10, 1, 76, 3, 1)]   // kalorie > 1500
    [InlineData(-1, 10, 1, 76, 3, 1)]     // kalorie < 0
    [InlineData(364, 101, 1, 76, 3, 1)]   // białko > 100 g
    [InlineData(364, 10, -1, 76, 3, 1)]   // tłuszcz < 0
    [InlineData(364, 10, 1, 200, 3, 1)]   // węglowodany > 100 g
    public async Task PopulateNutritionAsync_WhenValuesImplausible_AddsNothing(
        double cal, double protein, double fat, double carbs, double fiber, double sugar)
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(cal, protein, fat, carbs, fiber, sugar));
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        Assert.Empty(ingredient.IngredientNutritions);
    }

    [Fact]
    public async Task PopulateNutritionAsync_WhenNaN_AddsNothing()
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(cal: double.NaN));
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        Assert.Empty(ingredient.IngredientNutritions);
    }

    [Theory]
    [InlineData(0)]       // dolna granica kalorii
    [InlineData(1500)]    // górna granica kalorii
    public async Task PopulateNutritionAsync_WhenAtBoundaries_AddsValues(double cal)
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(cal: cal, protein: 100, fat: 0, carbs: 0, fiber: 0, sugar: 0));
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        Assert.NotEmpty(ingredient.IngredientNutritions); // granice są dopuszczalne
    }

    // -----------------------------------------------------------------------
    // Poprawne dane — dopięcie 6 wartości + zaokrąglenia
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PopulateNutritionAsync_WhenPlausible_AddsSixNutritionRows()
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts());
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        // Kalorie, Białko, Tłuszcz, Węglowodany, Błonnik, Cukry = 6
        Assert.Equal(6, ingredient.IngredientNutritions.Count);
        // typy 1..6, bez duplikatów
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 },
            ingredient.IngredientNutritions.Select(n => n.NutritionTypeId).OrderBy(x => x));
    }

    [Fact]
    public async Task PopulateNutritionAsync_RoundsAmountsToTwoDecimals()
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(cal: 364.456, protein: 10.123));
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        var calories = ingredient.IngredientNutritions.Single(n => n.NutritionTypeId == 1);
        var protein = ingredient.IngredientNutritions.Single(n => n.NutritionTypeId == 2);
        Assert.Equal(364.46, calories.AmountPer100g);
        Assert.Equal(10.12, protein.AmountPer100g);
    }

    // -----------------------------------------------------------------------
    // Przeliczniki jednostek (gęstość, waga sztuki)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PopulateNutritionAsync_WhenDensityInRange_SetsRoundedDensity()
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(density: 1.0309));
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        Assert.Equal(1.031, ingredient.DensityGramsPerMl); // zaokrąglone do 3 miejsc
    }

    [Theory]
    [InlineData(0)]      // 0 = nie dotyczy
    [InlineData(-1)]     // ujemne
    [InlineData(6)]      // poza górną granicą (>5)
    public async Task PopulateNutritionAsync_WhenDensityOutOfRange_LeavesNull(double density)
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(density: density));
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        Assert.Null(ingredient.DensityGramsPerMl);
    }

    [Fact]
    public async Task PopulateNutritionAsync_WhenGramsPerPieceInRange_SetsRoundedValue()
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(gramsPerPiece: 58.37));
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        Assert.Equal(58.4, ingredient.GramsPerPiece); // zaokrąglone do 1 miejsca
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5001)]   // > 5000
    public async Task PopulateNutritionAsync_WhenGramsPerPieceOutOfRange_LeavesNull(double grams)
    {
        _provider.Setup(p => p.FetchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facts(gramsPerPiece: grams));
        var ingredient = NewIngredient();
        var svc = CreateService();

        await svc.PopulateNutritionAsync(ingredient);

        Assert.Null(ingredient.GramsPerPiece);
    }
}
