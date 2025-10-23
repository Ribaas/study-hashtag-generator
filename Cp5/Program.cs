using Cp5;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var AVAILABLE_MODELS = new string[] { "gemma3:270m" };

app.MapPost("/hashtags", (HashtagRequest payload) =>
{
	// Set defaults if missing or invalid
	var model = string.IsNullOrWhiteSpace(payload.model) ? "gemma3:270m" : payload.model;
	if (!AVAILABLE_MODELS.Contains(model))
	{
		return Results.BadRequest($"Model '{model}' is not supported.");
	}
	var count = payload.count <= 0 ? 10 : payload.count;

	return Results.Ok($"text={payload.text}, count={count}, model={model}");
});

app.Run();