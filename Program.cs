using AnkiMakerBE.Models;
using AnkiMakerBE.Services;
using DotNetEnv;


var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
    }
}

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Dodaj CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Dodaj serwisy
builder.Services.AddHttpClient<IContentExtractorService, ContentExtractorService>();
builder.Services.AddSingleton<IAnkiGeneratorService, AnkiGeneratorService>();

var app = builder.Build();

app.UseCors("AllowReactApp");

app.MapPost("/api/process", async (
    ProcessUrlRequest request,
    IContentExtractorService contentExtractor,
    IAnkiGeneratorService ankiGenerator,
    ILogger<Program> logger) =>
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return Results.BadRequest(new { error = "URL is required" });
            }
            logger.LogInformation("Processing URL: {Url}", request.Url);
            // Pobierz zawartość strony
            var content = await contentExtractor.ExtractContentAsync(request.Url);
            if (string.IsNullOrWhiteSpace(content))
            {
                return Results.BadRequest(new { error = "Could not extract content from URL" });
            }
            // Wygeneruj podsumowanie i fiszki Anki
            var result = await ankiGenerator.GenerateAnkiCardsAsync(content);
            logger.LogInformation("Generated {Count} Anki cards", result.AnkiCards.Count);
            return Results.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error fetching URL");
            return Results.BadRequest(new { error = "Could not fetch the URL. Please check if it's valid and accessible." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing request");
            return Results.Problem("An error occurred while processing the request");
        }
    });

app.MapGet("/", () => "Anki Maker API is running!");
app.Run();
