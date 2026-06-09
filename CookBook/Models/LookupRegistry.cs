namespace CookBook.Models;

/// <summary>Opis pojedynczego słownika w panelu moderacji.</summary>
/// <param name="Slug">Identyfikator w URL, np. "tags".</param>
/// <param name="Singular">Forma pojedyncza do komunikatów ("Dodano tag.").</param>
/// <param name="Plural">Nazwa wyświetlana na zakładce/nagłówku.</param>
/// <param name="Emoji">Ikonka zakładki.</param>
/// <param name="EntityType">Typ encji EF (musi implementować INamedEntity).</param>
public record LookupDescriptor(string Slug, string Singular, string Plural, string Emoji, Type EntityType);

/// <summary>
/// Rejestr słowników zarządzanych w panelu moderacji.
/// Dodanie nowego słownika = dopisanie jednej linijki tutaj
/// (encja musi implementować INamedEntity).
/// </summary>
public static class LookupRegistry
{
    public static readonly IReadOnlyList<LookupDescriptor> All = new[]
    {
        new LookupDescriptor("categories",        "kategorię",        "Kategorie",         "🏷️", typeof(Category)),
        new LookupDescriptor("tags",              "tag",              "Tagi",              "#️⃣", typeof(Tag)),
        new LookupDescriptor("units",             "jednostkę",        "Jednostki",         "📏", typeof(Unit)),
        new LookupDescriptor("difficulty-levels", "poziom trudności", "Poziomy trudności", "📊", typeof(DifficultyLevel)),
        new LookupDescriptor("allergens",         "alergen",          "Alergeny",          "⚠️", typeof(Allergen)),
        new LookupDescriptor("nutrition-types",   "typ wartości odżywczej", "Wartości odżywcze", "🥗", typeof(NutritionType)),
    };

    public static LookupDescriptor? Find(string? slug) =>
        All.FirstOrDefault(d => d.Slug == slug);
}
