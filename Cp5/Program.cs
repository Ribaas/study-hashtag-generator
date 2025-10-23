using Cp5;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var AVAILABLE_MODELS = new string[] { "gemma3:270m" };

app.MapPost("/hashtags", async (HashtagRequest payload) =>
{
	var model = string.IsNullOrWhiteSpace(payload.model) ? "gemma3:270m" : payload.model;
	if (!AVAILABLE_MODELS.Contains(model))
	{
		return Results.BadRequest($"Model '{model}' is not supported.");
	}
	var count = payload.count <= 0 ? 10 : payload.count;


	var client = new HttpClient();
	var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate");

	var prompt = $"Generate a list of {count} hashtags for the given text. Respond using JSON (array of strings named hashtags). Text: {payload.text}.";

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
	Console.WriteLine(ollamaResponse);

	return Results.Ok($"text={payload.text}, count={count}, model={model}");
});

app.Run();