using System.Text.Json.Serialization;

namespace CookBook.Dtos;

// Odpowiedź Mistral Chat Completions — interesuje nas tylko treść pierwszej wiadomości.
internal record MistralResponse(
    [property: JsonPropertyName("choices")] List<MistralChoice>? Choices);

internal record MistralChoice(
    [property: JsonPropertyName("message")] MistralMessage? Message);

internal record MistralMessage(
    [property: JsonPropertyName("content")] string? Content);

// JSON zwracany przez model w treści wiadomości (response_format: json_schema).
internal record NutritionPayload(
    [property: JsonPropertyName("recognized")] bool Recognized,
    [property: JsonPropertyName("calories")] double Calories,
    [property: JsonPropertyName("protein")] double Protein,
    [property: JsonPropertyName("fat")] double Fat,
    [property: JsonPropertyName("carbs")] double Carbs,
    [property: JsonPropertyName("fiber")] double Fiber,
    [property: JsonPropertyName("sugar")] double Sugar,
    [property: JsonPropertyName("densityGramsPerMl")] double DensityGramsPerMl,
    [property: JsonPropertyName("gramsPerPiece")] double GramsPerPiece);
