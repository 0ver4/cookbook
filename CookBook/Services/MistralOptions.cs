namespace CookBook.Services;

/// <summary>Konfiguracja dostawcy wartości odżywczych opartego o Mistral (sekcja "Mistral").</summary>
public class MistralOptions
{
    /// <summary>Globalny włącznik feature'u. Gdy false — provider nie woła API.</summary>
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://api.mistral.ai";
    public string Model { get; set; } = "mistral-medium-latest";
    public string ApiKey { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 15;
}
