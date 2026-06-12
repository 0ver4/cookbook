namespace CookBook.Dtos;

/// <summary>
/// Generyczny wynik stronicowania - lista elementów bieżącej strony wraz z metadanymi
/// (numer strony, rozmiar strony, łączna liczba rekordów). Używany do paginacji list.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>Łączna liczba stron (zaokrąglona w górę).</summary>
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
