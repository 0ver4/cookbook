using System.ComponentModel.DataAnnotations;

namespace CookBook.Dtos;

/// <summary>Model do odczytu zwracany do widoków - nie pokazujemy encji EF bezpośrednio.</summary>
public record CategoryDto(int Id, string Name);

public record CreateCategoryDto
{
    [Required(ErrorMessage = "Nazwa jest wymagana.")]
    [MaxLength(50)]
    [Display(Name = "Nazwa")]
    public string Name { get; init; } = null!;
}

public record UpdateCategoryDto
{
    public int Id { get; init; }

    [Required(ErrorMessage = "Nazwa jest wymagana.")]
    [MaxLength(50)]
    [Display(Name = "Nazwa")]
    public string Name { get; init; } = null!;
}
