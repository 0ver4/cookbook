using CookBook.Models;

namespace CookBook.Dtos;

/// <summary>Lista pozycji słownika dla widoku Index.</summary>
public record LookupListVm(LookupDescriptor Descriptor, IReadOnlyList<(int Id, string Name)> Items);

/// <summary>Formularz dodawania/edycji pozycji słownika (Id = 0 → nowa pozycja).</summary>
public record LookupFormVm(LookupDescriptor Descriptor, int Id, string Name);
