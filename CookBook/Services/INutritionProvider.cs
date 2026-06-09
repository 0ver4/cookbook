namespace CookBook.Services;

/// <summary>
/// Wartości odżywcze na 100 g (kalorie w kcal, reszta w gramach) oraz przeliczniki jednostek
/// (gęstość g/ml i waga sztuki w gramach; 0 = nie dotyczy).
/// </summary>
public record NutritionFacts(
    double Calories, double Protein, double Fat, double Carbs, double Fiber, double Sugar,
    double Density, double GramsPerPiece);

/// <summary>
/// Źródło wartości odżywczych dla nazwy składnika. Implementacja jest wymienna
/// (obecnie Mistral LLM; można podmienić np. na Open Food Facts).
/// </summary>
public interface INutritionProvider
{
    /// <summary>Zwraca wartości na 100 g lub null, gdy nierozpoznane / wyłączone / błąd.</summary>
    Task<NutritionFacts?> FetchAsync(string ingredientName, CancellationToken ct = default);
}
