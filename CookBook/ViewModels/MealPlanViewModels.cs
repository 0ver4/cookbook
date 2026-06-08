using CookBook.Models;

namespace CookBook.ViewModels;

/// <summary>Polskie etykiety i kolejność slotów posiłków.</summary>
public static class MealTypeInfo
{
    public static readonly IReadOnlyList<MealType> Ordered = new[]
    {
        MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack
    };

    public static string Label(MealType type) => type switch
    {
        MealType.Breakfast => "Śniadanie",
        MealType.Lunch => "Obiad",
        MealType.Dinner => "Kolacja",
        MealType.Snack => "Przekąska",
        _ => type.ToString()
    };
}

/// <summary>Pojedynczy zaplanowany posiłek w komórce kalendarza.</summary>
public record MealPlanEntry(int ItemId, int RecipeId, string RecipeName);

/// <summary>Jeden dzień tygodnia z posiłkami pogrupowanymi po slocie.</summary>
public class MealPlanDay
{
    public DateTime Date { get; set; }
    public Dictionary<MealType, List<MealPlanEntry>> Meals { get; set; } = new();
}

/// <summary>Widok tygodniowego planu posiłków.</summary>
public class MealPlanWeekViewModel
{
    public DateTime WeekStart { get; set; }
    public DateTime PreviousWeek => WeekStart.AddDays(-7);
    public DateTime NextWeek => WeekStart.AddDays(7);

    public List<MealPlanDay> Days { get; set; } = new();
    public List<LookupItem> Recipes { get; set; } = new();
}
