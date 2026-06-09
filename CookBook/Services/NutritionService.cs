using CookBook.Models;

namespace CookBook.Services;

public class NutritionService : INutritionService
{
    // Stałe id z seedu NutritionType (Data/CookBookContext.cs).
    private const int Calories = 1, Protein = 2, Fat = 3, Carbs = 4, Fiber = 5, Sugar = 6;

    // Granice sensowności na 100 g: makroskładniki nie przekroczą 100 g, kalorie ~do 1500 kcal.
    private const double MaxGrams = 100;
    private const double MaxCalories = 1500;

    private readonly INutritionProvider _provider;
    private readonly ILogger<NutritionService> _logger;

    public NutritionService(INutritionProvider provider, ILogger<NutritionService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task PopulateNutritionAsync(Ingredient ingredient, CancellationToken ct = default)
    {
        try
        {
            var facts = await _provider.FetchAsync(ingredient.Name, ct);
            if (facts is null)
            {
                _logger.LogInformation("Brak wartości odżywczych dla '{Name}'.", ingredient.Name);
                return;
            }

            if (!IsPlausible(facts))
            {
                _logger.LogWarning("Odrzucono niepoprawne wartości odżywcze dla '{Name}': {Facts}", ingredient.Name, facts);
                return;
            }

            AddValue(ingredient, Calories, facts.Calories);
            AddValue(ingredient, Protein, facts.Protein);
            AddValue(ingredient, Fat, facts.Fat);
            AddValue(ingredient, Carbs, facts.Carbs);
            AddValue(ingredient, Fiber, facts.Fiber);
            AddValue(ingredient, Sugar, facts.Sugar);

            // Przeliczniki jednostek (do sumowania wartości dla całego przepisu).
            ingredient.DensityGramsPerMl = facts.Density is > 0 and <= 5 ? Math.Round(facts.Density, 3) : null;
            ingredient.GramsPerPiece = facts.GramsPerPiece is > 0 and <= 5000 ? Math.Round(facts.GramsPerPiece, 1) : null;

            _logger.LogInformation(
                "Wartości odżywcze dla '{Name}' (na 100 g): {Kcal} kcal, B {P} g, T {F} g, W {C} g, błonnik {Fi} g, cukry {Su} g; gęstość {D} g/ml, sztuka {G} g.",
                ingredient.Name, facts.Calories, facts.Protein, facts.Fat, facts.Carbs, facts.Fiber, facts.Sugar,
                ingredient.DensityGramsPerMl, ingredient.GramsPerPiece);
        }
        catch (Exception ex)
        {
            // Graceful: pobieranie wartości nie może wywalić zapisu przepisu.
            _logger.LogWarning(ex, "Nieoczekiwany błąd przy pobieraniu wartości odżywczych dla '{Name}'.", ingredient.Name);
        }
    }

    private static void AddValue(Ingredient ingredient, int nutritionTypeId, double amount) =>
        ingredient.IngredientNutritions.Add(new IngredientNutrition
        {
            NutritionTypeId = nutritionTypeId,
            AmountPer100g = Math.Round(amount, 2)
        });

    private static bool IsPlausible(NutritionFacts f)
    {
        double[] grams = { f.Protein, f.Fat, f.Carbs, f.Fiber, f.Sugar };
        if (double.IsNaN(f.Calories) || f.Calories < 0 || f.Calories > MaxCalories)
            return false;
        return grams.All(g => !double.IsNaN(g) && g >= 0 && g <= MaxGrams);
    }
}
