using System.Text.Json;
using HashtagGenerator.Models;

namespace HashtagGenerator.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _ollamaUrl;

    public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _ollamaUrl = configuration["Ollama:Url"] ?? "http://localhost:11434";
    }

    public async Task<string[]?> GenerateHashtagsAsync(string text, string model, int count)
    {
        var prompt = $"Generate a list of {count} hashtags for the given text, preferably in its language. Respond using JSON (array of strings named hashtags). Text: {text}.";

        var requestBody = new OllamaRequest(
            Model: model,
            Prompt: prompt,
            Stream: false,
            Format: new
            {
                type = "object",
                properties = new
                {
                    hashtags = new
                    {
                        type = "array",
                        items = new { type = "string" }
                    }
                },
                required = new[] { "hashtags" }
            }
        );

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_ollamaUrl}/api/generate")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Ollama raw response: {Response}", ollamaResponse);

            return ParseHashtags(ollamaResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with Ollama service at {Url}", _ollamaUrl);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while generating hashtags");
            throw;
        }
    }

    private string[]? ParseHashtags(string ollamaResponse)
    {
        try
        {
            var doc = JsonDocument.Parse(ollamaResponse);
            if (doc.RootElement.TryGetProperty("response", out var responseProp))
            {
                var hashtagsDoc = JsonDocument.Parse(responseProp.GetString() ?? "{}");
                if (hashtagsDoc.RootElement.TryGetProperty("hashtags", out var hashtagsProp) &&
                    hashtagsProp.ValueKind == JsonValueKind.Array)
                {
                    return hashtagsProp.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(s => s != null)
                        .ToArray()!;
                }
            }

            _logger.LogWarning("Could not find 'hashtags' property in Ollama response");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse hashtags from Ollama response");
            return null;
        }
    }
}
