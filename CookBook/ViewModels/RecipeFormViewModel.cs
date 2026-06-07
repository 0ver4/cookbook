using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CookBook.ViewModels;

/// <summary>Prosta pozycja słownikowa do list rozwijanych i checkboxów.</summary>
public record LookupItem(int Id, string Name);

/// <summary>Istniejące zdjęcie pokazywane przy edycji.</summary>
public record ExistingImage(int ImageId, string Url);

public class RecipeIngredientInput
{
    public int IngredientId { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Ilość musi być większa od zera.")]
    public double Amount { get; set; }

    public int? UnitId { get; set; }
}

public class RecipeStepInput
{
    [Required(ErrorMessage = "Treść kroku jest wymagana.")]
    [MaxLength(2000)]
    public string Content { get; set; } = null!;
}

/// <summary>
/// Model formularza tworzenia/edycji przepisu. Nosi zarówno pola edytowalne,
/// jak i dane słownikowe potrzebne do wyrenderowania kontrolek.
/// </summary>
public class RecipeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa jest wymagana.")]
    [MaxLength(150)]
    [Display(Name = "Nazwa")]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    [Display(Name = "Opis")]
    public string? Description { get; set; }

    [Range(0, 10000)]
    [Display(Name = "Czas przygotowania (min)")]
    public int? PrepTimeMinutes { get; set; }

    [Range(0, 10000)]
    [Display(Name = "Czas gotowania (min)")]
    public int? CookTimeMinutes { get; set; }

    [Range(1, 100)]
    [Display(Name = "Porcje")]
    public int? Servings { get; set; }

    [Display(Name = "Poziom trudności")]
    public int DifficultyLevelId { get; set; }

    public List<RecipeIngredientInput> Ingredients { get; set; } = new();
    public List<RecipeStepInput> Steps { get; set; } = new();

    public List<int> SelectedCategoryIds { get; set; } = new();
    public List<int> SelectedTagIds { get; set; } = new();

    [Display(Name = "Zdjęcia")]
    public List<IFormFile>? NewImages { get; set; }

    // Zaznaczone do usunięcia przy edycji
    public List<int> RemoveImageIds { get; set; } = new();

    // Dane słownikowe wypełniane po stronie serwera (nie wiązane z formularza)
    public List<LookupItem> DifficultyLevels { get; set; } = new();
    public List<LookupItem> AllIngredients { get; set; } = new();
    public List<LookupItem> AllUnits { get; set; } = new();
    public List<LookupItem> AllCategories { get; set; } = new();
    public List<LookupItem> AllTags { get; set; } = new();
    public List<ExistingImage> ExistingImages { get; set; } = new();
}
