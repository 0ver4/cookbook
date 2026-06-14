using System.Globalization;
using System.Text;
using Bogus;
using CookBook.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CookBook.Data;

/// <summary>
/// Generator danych testowych (Bogus / Faker) - 1000 użytkowników, ~180 składników i 1000 przepisów.
/// Uruchamiany TYLKO w środowisku Development i tylko gdy baza jest pusta (brak przepisów).
/// Składniki i kroki losowane z pul, nazwy przepisów składane z szablonu (baza + dopełnienie).
/// </summary>
public static class FakeDataSeeder
{
    private const int UserCount = 1000;
    private const int RecipeCount = 1000;
    private const string SharedPassword = "Haslo123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<CookBookContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("FakeDataSeeder");

        // Idempotencja: jeśli są już jakieś przepisy, uznajemy bazę za zaseedowaną.
        if (await context.Recipes.AnyAsync())
            return;

        logger.LogInformation("FakeDataSeeder: generowanie danych testowych...");

        var userIds = await SeedUsersAsync(context, logger);
        var ingredientIds = await SeedIngredientsAsync(context, logger);
        await SeedRecipesAsync(context, userIds, ingredientIds, logger);
        await SeedEngagementAsync(context, userIds, logger);

        logger.LogInformation("FakeDataSeeder: zakończono.");
    }

    // ---------------------------------------------------------------------
    // Użytkownicy
    // ---------------------------------------------------------------------
    private static async Task<List<int>> SeedUsersAsync(CookBookContext context, ILogger logger)
    {
        var hasher = new PasswordHasher<ApplicationUser>();
        var faker = new Faker("pl");

        var users = new List<ApplicationUser>(UserCount);
        for (var i = 0; i < UserCount; i++)
        {
            var first = faker.Name.FirstName();
            var last = faker.Name.LastName();
            // E-mail bez polskich znaków + indeks gwarantuje unikalność (NormalizedEmail jest UNIQUE).
            var email = $"{Slug(first)}.{Slug(last)}{i + 1}@example.com";

            var user = new ApplicationUser
            {
                FirstName = first,
                LastName = last,
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                AccountCreated = faker.Date.Past(2).ToUniversalTime(),
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                LockoutEnabled = true
            };
            // Hasło hashowane per-user (PBKDF2 z losowym saltem) - identyczne hasło, różne hashe.
            user.PasswordHash = hasher.HashPassword(user, SharedPassword);
            users.Add(user);
        }

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        var userIds = users.Select(u => u.Id).ToList();

        // Przypisanie roli "User" wszystkim (rola zaseedowana wcześniej w SeedData).
        var userRoleId = await context.Roles.Where(r => r.Name == "User").Select(r => r.Id).FirstAsync();
        context.UserRoles.AddRange(userIds.Select(id => new IdentityUserRole<int> { UserId = id, RoleId = userRoleId }));
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        logger.LogInformation("FakeDataSeeder: dodano {Count} użytkowników.", userIds.Count);
        return userIds;
    }

    // ---------------------------------------------------------------------
    // Składniki (Ingredient.Name jest UNIQUE - pula musi być unikalna)
    // ---------------------------------------------------------------------
    private static async Task<List<int>> SeedIngredientsAsync(CookBookContext context, ILogger logger)
    {
        var faker = new Faker("pl");
        var ingredients = IngredientPool.Distinct().Select(name => new Ingredient
        {
            Name = name,
            UnitId = faker.Random.Int(1, 6),                                   // gram..łyżka (zob. seed Unit)
            GramsPerPiece = faker.Random.Bool(0.3f) ? faker.Random.Double(20, 300) : null
        }).ToList();

        context.Ingredients.AddRange(ingredients);
        await context.SaveChangesAsync();

        var ids = ingredients.Select(i => i.Id).ToList();
        context.ChangeTracker.Clear();
        logger.LogInformation("FakeDataSeeder: dodano {Count} składników.", ids.Count);
        return ids;
    }

    // ---------------------------------------------------------------------
    // Przepisy (z krokami, składnikami, kategoriami, tagami)
    // ---------------------------------------------------------------------
    private static async Task SeedRecipesAsync(
        CookBookContext context, List<int> userIds, List<int> ingredientIds, ILogger logger)
    {
        var faker = new Faker("pl");
        const int batchSize = 200;

        for (var i = 0; i < RecipeCount; i++)
        {
            var stepCount = faker.Random.Int(3, 8);
            var steps = faker.PickRandom(StepPool, Math.Min(stepCount, StepPool.Length))
                .Select((content, idx) => new RecipeStep { Order = idx + 1, Content = content })
                .ToList();

            var ingredientCount = faker.Random.Int(3, 10);
            var recipeIngredients = faker.PickRandom(ingredientIds, Math.Min(ingredientCount, ingredientIds.Count))
                .Select(ingId => new RecipeIngredient
                {
                    IngredientId = ingId,
                    Amount = Math.Round(faker.Random.Double(1, 500), 1),
                    UnitId = faker.Random.Int(1, 6)
                })
                .ToList();

            var categories = faker.PickRandom(Enumerable.Range(1, 7), faker.Random.Int(1, 3))
                .Select(catId => new RecipeCategory { CategoryId = catId }).ToList();

            var tags = faker.PickRandom(Enumerable.Range(1, 9), faker.Random.Int(0, 4))
                .Select(tagId => new RecipeTag { TagId = tagId }).ToList();

            var recipe = new Recipe
            {
                Name = $"{faker.PickRandom(NameBases)} {faker.PickRandom(NameQualifiers)}",
                Description = faker.Lorem.Sentences(faker.Random.Int(1, 3)),
                PrepTimeMinutes = faker.Random.Int(5, 60),
                CookTimeMinutes = faker.Random.Int(5, 180),
                Servings = faker.Random.Int(1, 8),
                IsPublished = faker.Random.Bool(0.9f),
                IsHidden = false,
                CreatedAt = faker.Date.Past(2).ToUniversalTime(),
                UserId = faker.PickRandom(userIds),
                DifficultyLevelId = faker.Random.Int(1, 3),
                Steps = steps,
                Ingredients = recipeIngredients,
                Categories = categories,
                Tags = tags
            };

            context.Recipes.Add(recipe);

            if ((i + 1) % batchSize == 0)
            {
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
                logger.LogInformation("FakeDataSeeder: przepisy {Done}/{Total}...", i + 1, RecipeCount);
            }
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        logger.LogInformation("FakeDataSeeder: dodano {Count} przepisów.", RecipeCount);
    }

    // ---------------------------------------------------------------------
    // Zaangażowanie: oceny, komentarze (+ odpowiedzi), reakcje na komentarze
    // ---------------------------------------------------------------------
    private static async Task SeedEngagementAsync(CookBookContext context, List<int> userIds, ILogger logger)
    {
        var faker = new Faker("pl");
        const int batchSize = 500;

        var recipes = await context.Recipes.Select(r => new { r.Id, r.CreatedAt }).ToListAsync();
        var reactionIds = await context.Reactions.Select(r => r.Id).ToListAsync();
        context.ChangeTracker.Clear();

        // Data zdarzenia: pomiędzy utworzeniem przepisu a teraz (żeby aktywność była "po" przepisie).
        DateTime After(DateTime from) => faker.Date.Between(from, DateTime.UtcNow);

        // --- 1) Oceny (Review) - klucz (RecipeId, UserId) => różni recenzenci na przepis ---
        var buffer = 0;
        foreach (var recipe in recipes)
        {
            var raters = faker.PickRandom(userIds, Math.Min(faker.Random.Int(0, 12), userIds.Count));
            foreach (var uid in raters)
            {
                context.Reviews.Add(new Review
                {
                    RecipeId = recipe.Id,
                    UserId = uid,
                    Rating = faker.Random.WeightedRandom(new[] { 1, 2, 3, 4, 5 }, new[] { 0.05f, 0.10f, 0.20f, 0.30f, 0.35f }),
                    CreatedAt = After(recipe.CreatedAt)
                });
                buffer++;
            }
            if (buffer >= batchSize) { await context.SaveChangesAsync(); context.ChangeTracker.Clear(); buffer = 0; }
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        logger.LogInformation("FakeDataSeeder: dodano oceny.");

        // --- 2) Komentarze top-level (trigger AFTER INSERT wygeneruje powiadomienia) ---
        buffer = 0;
        foreach (var recipe in recipes)
        {
            var count = faker.Random.Int(0, 6);
            for (var k = 0; k < count; k++)
            {
                context.Comments.Add(new Comment
                {
                    RecipeId = recipe.Id,
                    UserId = faker.PickRandom(userIds),
                    Content = faker.PickRandom(CommentPool),
                    ReplyToId = null,
                    CreatedAt = After(recipe.CreatedAt)
                });
                buffer++;
            }
            if (buffer >= batchSize) { await context.SaveChangesAsync(); context.ChangeTracker.Clear(); buffer = 0; }
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        logger.LogInformation("FakeDataSeeder: dodano komentarze.");

        // --- 3) Odpowiedzi na ~30% komentarzy (ten sam RecipeId, ReplyToId = rodzic) ---
        var topComments = await context.Comments
            .Where(c => c.ReplyToId == null)
            .Select(c => new { c.Id, c.RecipeId, c.CreatedAt })
            .ToListAsync();
        context.ChangeTracker.Clear();

        buffer = 0;
        foreach (var parent in topComments)
        {
            if (!faker.Random.Bool(0.3f)) continue;
            var replyCount = faker.Random.Int(1, 2);
            for (var k = 0; k < replyCount; k++)
            {
                context.Comments.Add(new Comment
                {
                    RecipeId = parent.RecipeId,
                    UserId = faker.PickRandom(userIds),
                    Content = faker.PickRandom(CommentPool),
                    ReplyToId = parent.Id,
                    CreatedAt = After(parent.CreatedAt)
                });
                buffer++;
            }
            if (buffer >= batchSize) { await context.SaveChangesAsync(); context.ChangeTracker.Clear(); buffer = 0; }
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        logger.LogInformation("FakeDataSeeder: dodano odpowiedzi.");

        // --- 4) Reakcje na komentarze - klucz (CommentId, UserId) => różni reagujący ---
        var commentIds = await context.Comments.Select(c => c.Id).ToListAsync();
        context.ChangeTracker.Clear();

        buffer = 0;
        foreach (var commentId in commentIds)
        {
            if (!faker.Random.Bool(0.4f)) continue;
            var reactors = faker.PickRandom(userIds, Math.Min(faker.Random.Int(1, 4), userIds.Count));
            foreach (var uid in reactors)
            {
                context.CommentReactions.Add(new CommentReaction
                {
                    CommentId = commentId,
                    UserId = uid,
                    ReactionId = faker.PickRandom(reactionIds)
                });
                buffer++;
            }
            if (buffer >= batchSize) { await context.SaveChangesAsync(); context.ChangeTracker.Clear(); buffer = 0; }
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        logger.LogInformation("FakeDataSeeder: dodano reakcje na komentarze.");
    }

    // ---------------------------------------------------------------------
    // Pomocnicze
    // ---------------------------------------------------------------------
    /// <summary>Usuwa polskie znaki diakrytyczne i zostawia litery/cyfry (do adresu e-mail).</summary>
    private static string Slug(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }
        // Łączymy ł/Ł itp., które nie rozkładają się przez normalizację.
        return sb.ToString().Replace("ł", "l").Replace("ø", "o");
    }

    // --- Pule danych ---

    private static readonly string[] NameBases =
    {
        "Zupa", "Krem", "Sałatka", "Kotlet", "Gulasz", "Pierogi", "Naleśniki", "Placki",
        "Zapiekanka", "Tarta", "Ciasto", "Sernik", "Muffinki", "Risotto", "Makaron",
        "Spaghetti", "Lasagne", "Pizza", "Tortilla", "Burger", "Kanapka", "Omlet",
        "Jajecznica", "Owsianka", "Smoothie", "Koktajl", "Pieczeń", "Roladki", "Klopsiki",
        "Frytki", "Curry", "Leczo", "Bigos", "Żurek", "Rosół", "Pasztet", "Mus", "Galaretka",
        "Pierożki", "Gołąbki"
    };

    private static readonly string[] NameQualifiers =
    {
        "z kurczakiem", "z wołowiną", "wieprzowy", "z łososiem", "z warzywami", "po wiejsku",
        "po francusku", "po włosku", "babci Halinki", "z grilla", "na ostro", "z serem",
        "z grzybami", "ze szpinakiem", "z dynią", "z cukinią", "z fasolą", "domowy",
        "ekspresowy", "fit", "wegański", "wegetariański", "bezglutenowy", "na słodko",
        "z miodem", "z czosnkiem", "z brokułami", "z batatami", "imprezowy", "tradycyjny"
    };

    private static readonly string[] CommentPool =
    {
        "Pyszne, robiłam wczoraj!", "Wyszło rewelacyjnie, polecam.", "Trochę za słone dla mnie.",
        "Dzięki za przepis!", "Mój ulubiony obiad ostatnio.", "Dodałem więcej czosnku i super.",
        "Dzieciaki zjadły ze smakiem.", "Proste i szybkie, idealne na wieczór.",
        "Zrobiłbym mniej cukru następnym razem.", "Genialne, dziękuję!", "Nie wyszło mi ciasto, za rzadkie.",
        "Można zamienić śmietanę na jogurt?", "Najlepszy przepis jaki znalazłem.", "Trochę pracochłonne, ale warto.",
        "Wygląda obłędnie!", "Robię regularnie, zawsze się udaje.", "Dodałam chili dla ostrości.",
        "Świetne na imprezę.", "Mąż był zachwycony.", "Polecam dorzucić zioła prowansalskie.",
        "Idealne proporcje.", "Za dużo soli według mnie.", "Pyszności, pięć gwiazdek!",
        "Czy można zamrozić?", "Wyszło lepiej niż w restauracji.", "Banalnie proste, dzięki!",
        "Dodałem ser i było super.", "Trochę suche, dolałbym bulionu.", "Klasyka, zawsze smakuje.",
        "Zrobione, rodzina zachwycona.", "Idealne na niedzielny obiad.", "Mega, robię drugi raz w tygodniu.",
        "Polecam świeże zioła zamiast suszonych.", "Wyszło aromatyczne i soczyste.", "Trochę za ostre dla mnie.",
        "Dziękuję, uratowało mi kolację!", "Proste składniki, świetny efekt.", "Najlepsze co jadłem od dawna.",
        "Dodałabym więcej warzyw.", "Smakowało wyśmienicie."
    };

    private static readonly string[] StepPool =
    {
        "Pokrój warzywa w drobną kostkę.", "Rozgrzej oliwę na patelni.",
        "Podsmaż cebulę na złoty kolor.", "Dodaj czosnek i smaż przez minutę.",
        "Zalej całość bulionem i zagotuj.", "Gotuj na małym ogniu przez 20 minut.",
        "Dopraw solą i pieprzem do smaku.", "Wymieszaj wszystkie składniki w misce.",
        "Ubij jajka z cukrem na puszystą masę.", "Przesiej mąkę i wymieszaj z proszkiem.",
        "Wlej ciasto do formy wyłożonej papierem.", "Piecz w 180 stopniach przez 40 minut.",
        "Odcedź makaron i przelej zimną wodą.", "Zetrzyj ser na tarce.",
        "Dodaj śmietanę i delikatnie zamieszaj.", "Posyp świeżą natką pietruszki.",
        "Skrop sokiem z cytryny.", "Marynuj mięso przez co najmniej godzinę.",
        "Smaż kotlety z obu stron na rumiano.", "Duś pod przykryciem przez 30 minut.",
        "Pokrój pieczywo na kromki.", "Rozłóż nadzienie równomiernie.",
        "Zwiń roladki i spnij wykałaczką.", "Wstaw do lodówki na 2 godziny.",
        "Podawaj na ciepło.", "Udekoruj listkami bazylii.",
        "Zblenduj zupę na gładki krem.", "Dodaj przyprawy i wymieszaj.",
        "Zagnieć ciasto na elastyczną kulę.", "Odstaw ciasto do wyrośnięcia.",
        "Rozwałkuj ciasto na cienki placek.", "Posmaruj sosem pomidorowym.",
        "Posyp startym serem.", "Pokrój w cienkie plasterki.",
        "Gotuj ziemniaki do miękkości.", "Rozgnieć ziemniaki na puree.",
        "Podsmaż grzyby do odparowania wody.", "Wlej śmietankę i zredukuj sos.",
        "Dopraw gałką muszkatołową.", "Wymieszaj sos z makaronem.",
        "Ostudź przed podaniem.", "Pokrój owoce na kawałki.",
        "Zalej jogurtem i wymieszaj.", "Przełóż do salaterki.",
        "Polej roztopionym masłem.", "Posyp prażonymi orzechami.",
        "Doprowadź do wrzenia, mieszając.", "Zmniejsz ogień i gotuj bez przykrycia.",
        "Przecedź przez sito.", "Podawaj z pieczywem."
    };

    private static readonly string[] IngredientPool =
    {
        "Mąka pszenna", "Mąka żytnia", "Mąka kukurydziana", "Cukier", "Cukier puder",
        "Cukier waniliowy", "Sól", "Pieprz czarny", "Pieprz biały", "Papryka słodka",
        "Papryka ostra", "Curry", "Kurkuma", "Imbir", "Cynamon", "Gałka muszkatołowa",
        "Liść laurowy", "Ziele angielskie", "Majeranek", "Oregano", "Bazylia", "Tymianek",
        "Rozmaryn", "Kolendra", "Kmin rzymski", "Czosnek", "Cebula", "Cebula czerwona",
        "Por", "Marchew", "Pietruszka korzeń", "Seler", "Ziemniaki", "Bataty", "Pomidory",
        "Pomidory koktajlowe", "Ogórek", "Ogórek kiszony", "Papryka czerwona",
        "Papryka żółta", "Papryka zielona", "Cukinia", "Bakłażan", "Dynia", "Brokuł",
        "Kalafior", "Kapusta biała", "Kapusta czerwona", "Kapusta kiszona", "Szpinak",
        "Sałata", "Rukola", "Roszponka", "Fasola czerwona", "Fasola biała", "Ciecierzyca",
        "Soczewica", "Groszek zielony", "Kukurydza", "Pieczarki", "Borowiki", "Kurki",
        "Jajka", "Mleko", "Śmietana 18%", "Śmietana 30%", "Śmietanka kremówka",
        "Jogurt naturalny", "Kefir", "Maślanka", "Masło", "Margaryna", "Ser żółty",
        "Ser feta", "Ser pleśniowy", "Mozzarella", "Parmezan", "Twaróg", "Serek mascarpone",
        "Serek ricotta", "Kurczak pierś", "Kurczak udka", "Wołowina", "Wieprzowina",
        "Mielone wieprzowe", "Mielone wołowe", "Boczek", "Szynka", "Kiełbasa", "Łosoś",
        "Dorsz", "Tuńczyk", "Krewetki", "Makaron spaghetti", "Makaron penne",
        "Makaron świderki", "Ryż biały", "Ryż brązowy", "Ryż basmati", "Kasza gryczana",
        "Kasza jaglana", "Kasza pęczak", "Płatki owsiane", "Bułka tarta", "Chleb",
        "Bułki", "Drożdże", "Proszek do pieczenia", "Soda oczyszczona", "Olej rzepakowy",
        "Oliwa z oliwek", "Olej kokosowy", "Ocet jabłkowy", "Ocet balsamiczny",
        "Sok z cytryny", "Miód", "Syrop klonowy", "Dżem truskawkowy", "Kakao",
        "Czekolada gorzka", "Czekolada mleczna", "Wiórki kokosowe", "Orzechy włoskie",
        "Orzechy laskowe", "Migdały", "Orzeszki ziemne", "Pestki dyni",
        "Słonecznik łuskany", "Sezam", "Rodzynki", "Żurawina suszona", "Jabłka", "Gruszki",
        "Banany", "Truskawki", "Maliny", "Borówki", "Jagody", "Cytryna", "Pomarańcza",
        "Limonka", "Winogrona", "Brzoskwinie", "Śliwki", "Wiśnie", "Ananas", "Mango",
        "Awokado", "Koncentrat pomidorowy", "Passata pomidorowa", "Bulion warzywny",
        "Bulion drobiowy", "Sos sojowy", "Musztarda", "Ketchup", "Majonez", "Chrzan",
        "Natka pietruszki", "Koperek", "Szczypiorek", "Kapary", "Oliwki", "Pesto"
    };
}
