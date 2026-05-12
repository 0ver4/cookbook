using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class Ingredient
{
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
    
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    
    public double? DensityGramsPerMl { get; set; }
    
    public ICollection<IngredientAllergen> IngredientAllergens { get; set; } = new List<IngredientAllergen>();
    public ICollection<IngredientNutrition> IngredientNutritions { get; set; } = new List<IngredientNutrition>();

}