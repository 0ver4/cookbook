namespace CookBook.Dtos;

/// <summary>Lista zakupów na widoku przeglądu.</summary>
public record ShoppingListSummaryDto(int Id, string Name, DateTime CreatedAt, int ItemCount, int CheckedCount);

/// <summary>Pozycja listy zakupów. Składnik + jednostka jednoznacznie identyfikują pozycję.</summary>
public record ShoppingListItemDto(int IngredientId, int UnitId, string IngredientName, double Amount, string UnitName, bool IsChecked);

/// <summary>Pełna lista zakupów ze szczegółami.</summary>
public record ShoppingListDetailsDto(
    int Id,
    string Name,
    DateTime CreatedAt,
    IReadOnlyList<ShoppingListItemDto> Items);
