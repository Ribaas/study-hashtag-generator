namespace Cp5.Models;

public record HashtagResponse(int Count, string Model, IReadOnlyList<string> Hashtags, string? Error = null);
