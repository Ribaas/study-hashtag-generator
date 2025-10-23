namespace Cp5.Models;

public record OllamaRequest(
    string Model,
    string Prompt,
    bool Stream,
    object Format
);
