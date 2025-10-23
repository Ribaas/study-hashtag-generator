namespace HashtagGenerator.Models;

public record OllamaRequest(
    string Model,
    string Prompt,
    bool Stream,
    object Format
);
