namespace CookBook.Models;

/// <summary>
/// Prosta encja słownikowa o kształcie { Id, Name }. Pozwala obsłużyć wszystkie
/// takie słowniki jednym generycznym serwisem i kontrolerem (LookupService, DictionaryController).
/// </summary>
public interface INamedEntity
{
    int Id { get; set; }
    string Name { get; set; }
}
