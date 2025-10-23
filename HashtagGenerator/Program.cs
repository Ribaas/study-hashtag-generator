using HashtagGenerator;
using HashtagGenerator.Models;
using HashtagGenerator.Services;
using HashtagGenerator.Validators;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddHttpClient<OllamaService>();
builder.Services.AddScoped<HashtagGeneratorService>();

var app = builder.Build();

app.MapPost("/hashtags", async (
    HashtagRequest request,
    HashtagGeneratorService hashtagGenerator,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Received hashtag generation request");

    // Validate request
    var (isValid, errorResponse) = HashtagRequestValidator.Validate(request);
    if (!isValid)
    {
        logger.LogWarning("Invalid request: {Error}", errorResponse!.Error);
        return Results.Json(errorResponse, statusCode: errorResponse.StatusCode);
    }

    var text = request.text.Trim();
    var model = HashtagRequestValidator.GetModelOrDefault(request.model);
    var count = request.count;

    try
    {
        // Generate hashtags
        var (hashtags, error) = await hashtagGenerator.GenerateHashtagsAsync(text, model, count);

        var response = new HashtagResponse(
            Count: count,
            Model: model,
            Hashtags: hashtags,
            Error: error
        );

        return Results.Ok(response);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Failed to connect to Ollama service");
        var response = new ErrorResponse(
            "Unable to connect to the AI service. Please ensure Ollama is running.",
            StatusCodes.Status503ServiceUnavailable
        );
        return Results.Json(response, statusCode: response.StatusCode);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during hashtag generation");
        var response = new ErrorResponse(
            "An unexpected error occurred while processing your request.",
            StatusCodes.Status500InternalServerError
        );
        return Results.Json(response, statusCode: response.StatusCode);
    }
});

app.Run();