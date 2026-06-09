# CookBook

Aplikacja webowa do zarządzania przepisami kulinarnymi, zbudowana w **ASP.NET Core 8 (MVC)**.
Użytkownicy mogą dodawać i przeglądać przepisy ze zdjęciami, komentować i oceniać je, organizować
przepisy w kolekcje, planować posiłki, generować listy zakupów (z eksportem do PDF) oraz korzystać
z automatycznie wyliczanych wartości odżywczych składników (zewnętrzne API LLM). Moderatorzy mają
dostęp do panelu moderacji (zgłoszenia treści, słowniki).

## Spis treści

- [Architektura](#architektura)
- [Zrealizowane funkcjonalności](#zrealizowane-funkcjonalności)
- [Wykorzystane biblioteki (wraz z wersjami)](#wykorzystane-biblioteki-wraz-z-wersjami)
- [Wymagania](#wymagania)
- [Instalacja i konfiguracja (środowisko deweloperskie)](#instalacja-i-konfiguracja-środowisko-deweloperskie)
- [Konfiguracja zewnętrznego API (Mistral)](#konfiguracja-zewnętrznego-api-mistral)
- [Uruchomienie](#uruchomienie)
- [Konta i role](#konta-i-role)
- [Testy automatyczne](#testy-automatyczne)
- [Wdrożenie produkcyjne (NixOS)](#wdrożenie-produkcyjne-nixos)
- [Autorzy](#autorzy)

## Architektura

Projekt stosuje **architekturę warstwową** z następującymi wzorcami:

- **Repository** — dostęp do danych odseparowany w warstwie repozytoriów
  (`Repositories/`); generyczne `IRepository<T>` / `Repository<T>` dla encji
  słownikowych oraz dedykowane repozytoria dla encji złożonych (przepisy, listy
  zakupów, plany posiłków).
- **Service layer** — logika biznesowa w serwisach (`Services/`), każda para
  `IXxxService` / `XxxService`.
- **Dependency Injection** — wszystkie repozytoria i serwisy rejestrowane w
  kontenerze DI w `Program.cs`.
- **DTO / ViewModel** — dane przekazywane do/z widoków przez obiekty
  transferowe (`Dtos/`, `ViewModels/`) zamiast bezpośredniego użycia encji.
- **ORM** — Entity Framework Core 8 z bazą Microsoft SQL Server, migracje w
  `Migrations/` (model obejmuje ~36 encji z relacjami One‑to‑Many oraz
  Many‑to‑Many, np. `RecipeToCollection`, `RecipeCategory`, `RecipeTag`,
  `IngredientAllergen`).

```
CookBook/
├── Controllers/            # kontrolery MVC (Recipe, Collection, MealPlan, ShoppingList, ...)
├── Areas/Moderation/       # obszar moderatora (zgłoszenia, słowniki)
├── Services/               # warstwa logiki biznesowej
├── Repositories/           # warstwa dostępu do danych (wzorzec Repository)
├── Models/                 # encje domenowe (EF Core)
├── Dtos/ , ViewModels/     # obiekty transferowe / modele widoków
├── Data/                   # DbContext + seedowanie danych
├── Migrations/             # migracje EF Core
├── ViewComponents/         # komponenty widoków (badge powiadomień / zgłoszeń)
├── Views/                  # widoki Razor
└── wwwroot/                # zasoby statyczne (Bootstrap, jQuery, uploady)
```

## Zrealizowane funkcjonalności

- **Przepisy (CRUD)** — tworzenie, edycja, przeglądanie, usuwanie przepisów wraz
  ze składnikami, krokami, kategoriami i tagami.
- **Przesyłanie plików** — wgrywanie zdjęć przepisów przez formularz HTML
  `file`; zawartość pliku jest zapisywana jako blob (`byte[]`) w bazie danych
  (encja `Image`) i serwowana przez endpoint `/Image/{id}`. Walidacja rozszerzeń
  (jpg/jpeg/png/webp) i rozmiaru (max 5 MB) w `ImageService`.
- **Komentarze, reakcje i recenzje** — komentowanie przepisów, reakcje
  (emoji) oraz oceny.
- **Kolekcje przepisów** — grupowanie przepisów w kolekcje (relacja
  Many‑to‑Many).
- **Plan posiłków** — przypisywanie przepisów do dni i typów posiłków.
- **Listy zakupów + eksport PDF** — generowanie list zakupów z możliwością
  pobrania jako **PDF** (QuestPDF).
- **Wartości odżywcze (zewnętrzne API)** — automatyczne wyliczanie wartości
  odżywczych składników przez **Mistral AI** za wymiennym interfejsem
  `INutritionProvider`.
- **Powiadomienia** — system powiadomień użytkownika.
- **Uwierzytelnianie i autoryzacja** — ASP.NET Core Identity (`ClaimsIdentity`)
  z dwiema rolami: **User** (zalogowany użytkownik) oraz **Moderator**
  (podwyższone uprawnienia).
- **Panel moderacji** — obszar `Moderation` z obsługą zgłoszeń treści
  (przepisy, komentarze) oraz zarządzaniem słownikami.
- **Estetyczny interfejs** — widoki Razor stylowane **Tailwind CSS** (responsywny
  layout, menu mobilne).

## Wykorzystane biblioteki (wraz z wersjami)

### Platforma

| Komponent          | Wersja             |
| ------------------ | ------------------ |
| .NET SDK / Runtime | **8.0** (`net8.0`) |
| ASP.NET Core       | 8.0 (MVC)          |

### Projekt główny (`CookBook`)

| Pakiet NuGet                                      | Wersja   |
| ------------------------------------------------- | -------- |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.10   |
| Microsoft.AspNetCore.Identity.UI                  | 8.0.10   |
| Microsoft.EntityFrameworkCore                     | 8.0.10   |
| Microsoft.EntityFrameworkCore.Design              | 8.0.10   |
| Microsoft.EntityFrameworkCore.SqlServer           | 8.0.10   |
| Microsoft.EntityFrameworkCore.Tools               | 8.0.10   |
| Microsoft.VisualStudio.Web.CodeGeneration.Design  | 8.0.6    |
| QuestPDF (generowanie PDF)                        | 2026.5.0 |

### Projekt testowy (`CookBook.Tests`)

| Pakiet NuGet                       | Wersja  |
| ---------------------------------- | ------- |
| xunit                              | 2.5.3   |
| xunit.runner.visualstudio          | 2.5.3   |
| Microsoft.NET.Test.Sdk             | 17.8.0  |
| Moq                                | 4.20.72 |
| MockQueryable.Moq                  | 7.0.0   |
| coverlet.collector (pokrycie kodu) | 6.0.0   |

### Biblioteki front‑end

- **Tailwind CSS** — ładowany przez CDN (`cdn.tailwindcss.com`) w `_Layout.cshtml`;
  podstawowy framework stylowania całej aplikacji.
- **jQuery** — `wwwroot/lib/jquery`.
- **jQuery Validation** + **jQuery Validation Unobtrusive** — walidacja
  formularzy po stronie klienta (`wwwroot/lib`).

## Wymagania

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- Serwer **Microsoft SQL Server** (np. lokalny, kontener Docker, lub zdalny)
- Klucz API do [Mistral AI](https://mistral.ai/) — opcjonalny, do funkcji
  wartości odżywczych

## Instalacja i konfiguracja (środowisko deweloperskie)

1. **Sklonuj repozytorium:**

   ```bash
   git clone git@github.com:0ver4/cookbook.git
   cd cookbook
   ```

2. **Skonfiguruj połączenie z bazą danych.**
   Edytuj `CookBook/appsettings.Development.json` i ustaw connection string
   `CookBookDb` na swój serwer SQL:

   ```json
   {
     "ConnectionStrings": {
       "CookBookDb": "Server=localhost,1433;Database=CookBook;User Id=sa;Password=Twoje_Haslo;TrustServerCertificate=True;Encrypt=False;"
     }
   }
   ```

   > Przykładowy serwer SQL w Dockerze:
   >
   > ```bash
   > docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Twoje_Mocne_Haslo1!" \
   >   -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
   > ```

3. **Przywróć zależności:**

   ```bash
   dotnet restore
   ```

4. **Zastosuj migracje (utworzenie schematu bazy danych):**

   ```bash
   dotnet ef database update --project CookBook
   ```

   > Jeśli nie masz narzędzia `dotnet-ef`, zainstaluj je:
   > `dotnet tool install --global dotnet-ef`

   Przy pierwszym uruchomieniu aplikacja automatycznie zaseeduje role
   (`User`, `Moderator`), konto systemowe oraz domyślne słowniki/reakcje
   (`Data/SeedData.cs`).

## Konfiguracja zewnętrznego API (Mistral)

Funkcja automatycznego wyliczania wartości odżywczych korzysta z API Mistral AI.
Ustaw klucz w `appsettings.Development.json` (lub przez zmienne środowiskowe):

```json
{
  "Mistral": {
    "Enabled": true,
    "ApiKey": "twoj-klucz-api"
  }
}
```

Pozostałe ustawienia (`BaseUrl`, `Model`, `TimeoutSeconds`) mają sensowne
wartości domyślne w `appsettings.json`. Ustaw `"Enabled": false`, aby wyłączyć
integrację.

> ℹ️ **Bezpieczeństwo:** plik `appsettings.Development.json` jest w `.gitignore`,
> więc lokalne klucze i hasła nie trafiają do repozytorium. W środowisku
> produkcyjnym sekrety podawaj przez zmienne środowiskowe lub menedżer sekretów.

## Uruchomienie

```bash
dotnet run --project CookBook
```

Aplikacja wystartuje pod adresem wypisanym w konsoli (domyślnie
`https://localhost:5001` / `http://localhost:5000`).

## Konta i role

Aplikacja wspiera dwie role:

- **User** — zalogowany użytkownik (zarządzanie własnymi przepisami,
  kolekcjami, planem posiłków, listami zakupów).
- **Moderator** — dodatkowo dostęp do obszaru `Moderation` (obsługa zgłoszeń,
  słowniki).

Przy seedowaniu tworzone jest konto systemowe z rolą moderatora:

- **e‑mail:** `system@cookbook.local`
- **hasło:** `System!123`

Pozostali użytkownicy rejestrują się samodzielnie przez stronę rejestracji
(domyślnie otrzymują rolę `User`).

## Testy automatyczne

Testy jednostkowe (xUnit + Moq) pokrywają główną logikę warstwy serwisów
(bez zapisów do ORM) — m.in. `RecipeService`, `CollectionService`,
`MealPlanService`, `ShoppingListService`, `NotificationService`,
`NutritionService`, `ReportService`, `LookupService`.

Uruchomienie testów:

```bash
dotnet test
```

Wygenerowanie raportu pokrycia kodu:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Wdrożenie produkcyjne

Aplikacja działa na produkcji pod adresem **https://cookbook.neoney.dev**.

Hostowana jest na **NixOS** jako usługa systemd. Repozytorium zawiera flake
Nix opisujący całość wdrożenia:

- `flake.nix` — pakiet (`packages.default`) budowany przez `buildDotnetModule`
  z `dotnet-sdk_8` oraz moduł NixOS (`nixosModules.default`).
- `nix/module.nix` — usługa systemd (`services.cookbook`) z utwardzeniem
  (DynamicUser, ProtectSystem itd.), konfigurowalnym adresem/portem oraz plikiem
  środowiskowym (`environmentFile`) na sekrety.

Sekrety produkcyjne (np. `ConnectionStrings__CookBookDb`, `Mistral__ApiKey`)
przekazywane są przez zmienne środowiskowe. TLS i reverse proxy (Caddy)
obsługiwane są poza modułem — aplikacja honoruje nagłówki `X-Forwarded-*`.

## Autorzy

Projekt zespołowy. Główne obszary odpowiedzialności:

- **Michał Minarowski**
- **Julia** — moduł powiadomień
- **Anna** — kolekcje / profil
