using HashtagGenerator.Models;

namespace HashtagGenerator.Validators;

public static class HashtagRequestValidator
{
    private static readonly string[] AvailableModels = { "gemma3:270m", "gemma3:1b" };

    public static (bool isValid, ErrorResponse? error) Validate(HashtagRequest request)
    {
        if (request.text == null || string.IsNullOrWhiteSpace(request.text))
        {
            return (false, new ErrorResponse("Text must not be null or empty.", StatusCodes.Status400BadRequest));
        }

        var trimmedText = request.text.Trim();
        if (string.IsNullOrWhiteSpace(trimmedText))
        {
            return (false, new ErrorResponse("Text must not be empty or whitespace.", StatusCodes.Status400BadRequest));
        }

        var model = string.IsNullOrWhiteSpace(request.model) ? "gemma3:270m" : request.model;
        if (!AvailableModels.Contains(model))
        {
            return (false, new ErrorResponse($"Model '{model}' is not supported. Available models: {string.Join(", ", AvailableModels)}",
                StatusCodes.Status400BadRequest));
        }

        return (true, null);
    }

    public static string GetModelOrDefault(string? model)
    {
        return string.IsNullOrWhiteSpace(model) ? "gemma3:270m" : model;
    }
}
