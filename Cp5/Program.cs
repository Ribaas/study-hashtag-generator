using Cp5;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var AVAILABLE_MODELS = new string[] { "gemma3:270m" };
var MAX_RETRIES = 10;
var MAX_HASHTAGS = 30;

app.MapPost("/hashtags", async (HashtagRequest payload) =>
{
    var model = string.IsNullOrWhiteSpace(payload.model) ? "gemma3:270m" : payload.model;
    if (!AVAILABLE_MODELS.Contains(model))
    {
        return Results.BadRequest($"Model '{model}' is not supported.");
    }

    var count = payload.count <= 0 ? 10 : payload.count;
    if (count > MAX_HASHTAGS)
    {
        count = MAX_HASHTAGS;
        Console.WriteLine($"Requested count exceeds {MAX_HASHTAGS}. Limiting to {MAX_HASHTAGS} hashtags.");
    }

    var client = new HttpClient();
    int retries = 0;
    var allHashtags = new List<string>();
    string error = null;

    while (allHashtags.Count < count && retries < MAX_RETRIES)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate");
        var prompt =
            $"Generate a list of {count} hashtags for the given text. Respond using JSON (array of strings named hashtags). Text: {payload.text}.";
        var requestBody = new
        {
            model = model,
            prompt = prompt,
            stream = false,
            format = new
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
        };
        var json = JsonSerializer.Serialize(requestBody);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var ollamaResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[Ollama raw response] {ollamaResponse}");


        string[] hashtags = null;
        try
        {
            var doc = JsonDocument.Parse(ollamaResponse);
            if (doc.RootElement.TryGetProperty("response", out var responseProp))
            {
                var hashtagsDoc = JsonDocument.Parse(responseProp.GetString());
                if (hashtagsDoc.RootElement.TryGetProperty("hashtags", out var hashtagsProp) &&
                    hashtagsProp.ValueKind == JsonValueKind.Array)
                {
                    hashtags = hashtagsProp.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).ToArray();
                }
            }
        }
        catch
        {
            Console.WriteLine("Failed to parse hashtags from Ollama response.");
            error = "Could not parse hashtags from Ollama response.";
        }

        Console.WriteLine($"[Ollama hashtags count] {hashtags?.Length ?? 0}");

        if (hashtags == null)
        {
            retries++;
            continue;
        }


        // Log hashtags containing spaces and filter them out
        var hashtagsWithSpaces = hashtags?.Where(h => h != null && h.Contains(' ')).ToArray() ?? Array.Empty<string>();
        if (hashtagsWithSpaces.Length > 0)
        {
            Console.WriteLine($"Filtered out hashtags with spaces: {string.Join(", ", hashtagsWithSpaces)}");
        }

        var filteredHashtags = hashtags == null
            ? Array.Empty<string>()
            : hashtags
                .Where(h => !string.IsNullOrWhiteSpace(h) && !h.Contains(' '))
                .Select(h => h.StartsWith("#") ? h : "#" + h)
                .ToArray();

        Console.WriteLine(
            $"[Filtered hashtags count] {filteredHashtags.Length}, [Total unique hashtags so far] {allHashtags.Count + filteredHashtags.Count(h => !allHashtags.Contains(h))}");

        // Add only new hashtags (avoid duplicates)
        foreach (var h in filteredHashtags)
        {
            if (!allHashtags.Contains(h))
                allHashtags.Add(h);
        }

        retries++;
    }

    // If more than requested, trim
    if (allHashtags.Count > count)
    {
        allHashtags = allHashtags.Take(count).ToList();
    }

    if (allHashtags.Count < count)
    {
        return Results.Ok(new
        {
            count, model, hashtags = allHashtags,
            error = error ?? $"Could not generate the requested number of hashtags after {MAX_RETRIES} attempts."
        });
    }

    return Results.Ok(new { count, model, hashtags = allHashtags });
});

app.Run();