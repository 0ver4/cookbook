using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CookBook.Dtos;
using Microsoft.Extensions.Options;

namespace CookBook.Services;

/// <summary>
/// Pobiera wartości odżywcze z Mistral (Chat Completions, structured output json_schema).
/// Nie rzuca wyjątków na zewnątrz — w razie problemu zwraca null.
/// </summary>
public class MistralNutritionProvider : INutritionProvider
{
    private const string SystemPrompt =
        "Jesteś bazą wartości odżywczych. Dla podanej nazwy składnika podaj wartości na 100 g: " +
        "calories w kcal, protein/fat/carbs/fiber/sugar w gramach. " +
        "Podaj też densityGramsPerMl (gęstość w g/ml) oraz gramsPerPiece (przybliżona waga jednej typowej sztuki w gramach; " +
        "0 jeśli składnika nie liczy się na sztuki, np. mąka). " +
        "Jeśli nazwa nie jest produktem spożywczym, ustaw recognized=false i wszystkie liczby na 0.";

    // Schemat wymuszany przez Mistral (response_format: json_schema, strict).
    private static readonly object ResponseSchema = new
    {
        type = "object",
        properties = new
        {
            recognized = new { type = "boolean" },
            calories = new { type = "number" },
            protein = new { type = "number" },
            fat = new { type = "number" },
            carbs = new { type = "number" },
            fiber = new { type = "number" },
            sugar = new { type = "number" },
            densityGramsPerMl = new { type = "number" },
            gramsPerPiece = new { type = "number" }
        },
        required = new[] { "recognized", "calories", "protein", "fat", "carbs", "fiber", "sugar", "densityGramsPerMl", "gramsPerPiece" },
        additionalProperties = false
    };

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly MistralOptions _options;
    private readonly ILogger<MistralNutritionProvider> _logger;

    public MistralNutritionProvider(HttpClient http, IOptions<MistralOptions> options, ILogger<MistralNutritionProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<NutritionFacts?> FetchAsync(string ingredientName, CancellationToken ct = default)
    {
        // No-op bez wywołania HTTP, gdy feature wyłączony lub brak klucza.
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        var body = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = ingredientName }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new { name = "nutrition", strict = true, schema = ResponseSchema }
            },
            temperature = 0
        };

        try
        {
            using var resp = await _http.PostAsJsonAsync("/v1/chat/completions", body, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mistral zwrócił {Status} dla składnika '{Name}'.", (int)resp.StatusCode, ingredientName);
                return null;
            }

            var parsed = await resp.Content.ReadFromJsonAsync<MistralResponse>(JsonOpts, ct);
            var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Mistral: pusta treść odpowiedzi dla '{Name}'.", ingredientName);
                return null;
            }

            var payload = JsonSerializer.Deserialize<NutritionPayload>(content, JsonOpts);
            if (payload is null || !payload.Recognized)
                return null;

            return new NutritionFacts(
                payload.Calories, payload.Protein, payload.Fat, payload.Carbs, payload.Fiber, payload.Sugar,
                payload.DensityGramsPerMl, payload.GramsPerPiece);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Błąd pobierania wartości odżywczych dla '{Name}'.", ingredientName);
            return null;
        }
    }
}
