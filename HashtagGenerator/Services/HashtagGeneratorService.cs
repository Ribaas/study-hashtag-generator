namespace HashtagGenerator.Services;

public class HashtagGeneratorService
{
    private readonly OllamaService _ollamaService;
    private readonly ILogger<HashtagGeneratorService> _logger;
    private readonly int _maxRetries;
    private readonly int _maxHashtags;

    public HashtagGeneratorService(
        OllamaService ollamaService,
        ILogger<HashtagGeneratorService> logger,
        IConfiguration configuration)
    {
        _ollamaService = ollamaService;
        _logger = logger;
        _maxRetries = configuration.GetValue<int>("HashtagGenerator:MaxRetries", 10);
        _maxHashtags = configuration.GetValue<int>("HashtagGenerator:MaxHashtags", 30);
    }

    public async Task<(List<string> hashtags, string? error)> GenerateHashtagsAsync(
        string text,
        string model,
        int requestedCount)
    {
        var count = NormalizeCount(requestedCount);
        var allHashtags = new List<string>();
        string? error = null;
        int retries = 0;

        _logger.LogInformation("Starting hashtag generation: Text length={TextLength}, Model={Model}, Count={Count}",
            text.Length, model, count);

        while (allHashtags.Count < count && retries < _maxRetries)
        {
            retries++;
            _logger.LogDebug("Attempt {Retry}/{MaxRetries} to generate hashtags", retries, _maxRetries);

            try
            {
                var hashtags = await _ollamaService.GenerateHashtagsAsync(text, model, count);

                if (hashtags == null)
                {
                    error = "Could not parse hashtags from Ollama response.";
                    continue;
                }

                _logger.LogDebug("Received {Count} raw hashtags from Ollama", hashtags.Length);

                var filteredHashtags = FilterAndNormalizeHashtags(hashtags);
                _logger.LogDebug("Filtered to {Count} valid hashtags", filteredHashtags.Count);

                AddUniqueHashtags(allHashtags, filteredHashtags);
                _logger.LogDebug("Total unique hashtags: {Count}", allHashtags.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hashtag generation attempt {Retry}", retries);
                error = "An error occurred while generating hashtags.";
            }
        }

        var finalHashtags = allHashtags.Take(count).ToList();

        if (finalHashtags.Count < count)
        {
            var warningMessage = $"Could not generate the requested number of hashtags after {_maxRetries} attempts.";
            _logger.LogWarning(warningMessage + " Requested={Requested}, Generated={Generated}",
                count, finalHashtags.Count);
            error = error ?? warningMessage;
        }
        else
        {
            _logger.LogInformation("Successfully generated {Count} hashtags", finalHashtags.Count);
        }

        return (finalHashtags, error);
    }

    private int NormalizeCount(int requestedCount)
    {
        if (requestedCount <= 0)
        {
            _logger.LogDebug("Invalid count {Count}, defaulting to 10", requestedCount);
            return 10;
        }

        if (requestedCount > _maxHashtags)
        {
            _logger.LogWarning("Requested count {Count} exceeds maximum {Max}, limiting to {Max}",
                requestedCount, _maxHashtags, _maxHashtags);
            return _maxHashtags;
        }

        return requestedCount;
    }

    private List<string> FilterAndNormalizeHashtags(string[] hashtags)
    {
        var hashtagsWithSpaces = hashtags.Where(h => h.Contains(' ')).ToList();
        if (hashtagsWithSpaces.Any())
        {
            _logger.LogDebug("Filtered out {Count} hashtags with spaces: {Hashtags}",
                hashtagsWithSpaces.Count, string.Join(", ", hashtagsWithSpaces));
        }

        return hashtags
            .Where(h => !string.IsNullOrWhiteSpace(h) && !h.Contains(' '))
            .Select(h => h.StartsWith("#") ? h : "#" + h)
            .ToList();
    }

    private void AddUniqueHashtags(List<string> allHashtags, List<string> newHashtags)
    {
        foreach (var hashtag in newHashtags)
        {
            if (!allHashtags.Contains(hashtag.ToLower()))
            {
                allHashtags.Add(hashtag.ToLower());
            }
        }
    }
}
