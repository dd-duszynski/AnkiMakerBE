using OpenAI;
using OpenAI.Chat;
using AnkiMakerApp.Models;
using System.Text.Json;

namespace AnkiMakerApp.Services;

public interface IAnkiGeneratorService
{
    Task<ProcessUrlResponse> GenerateAnkiCardsAsync(string content);
}

public class AnkiGeneratorService : IAnkiGeneratorService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<AnkiGeneratorService> _logger;

    public AnkiGeneratorService(IConfiguration configuration, ILogger<AnkiGeneratorService> logger)
    {
        var apiKey = configuration["OPENAI_API_KEY"] ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable not configured");
        var client = new OpenAIClient(apiKey);
        _chatClient = client.GetChatClient("gpt-4o");
        _logger = logger;
    }

    public async Task<ProcessUrlResponse> GenerateAnkiCardsAsync(string content)
    {
        try
        {
            var prompt = $@"Jesteś ekspertem w tworzeniu materiałów edukacyjnych dla programistów full-stack (TypeScript/React/.NET/CSS).
            
            Przeanalizuj poniższy artykuł i wykonaj dwa zadania:
            1. Stwórz zwięzłe podsumowanie artykułu (2-3 zdania) skupiające się na najważniejszych koncepcjach.
            2. Wygeneruj od 1 do 3 fiszek Anki (liczba zależna od złożoności tematu):
            - Dla prostych tematów: 1 fiszka
            - Dla średnio złożonych: 2 fiszki
            - Dla złożonych tematów: 3 fiszki
               Każda fiszka powinna zawierać:
                - Przód (front): Pytanie lub pojęcie do zapamiętania
                - Tył (back): Jasna, zwięzła odpowiedź z praktycznym przykładem jeśli to możliwe
            WAŻNE: Odpowiedz TYLKO w formacie JSON bez dodatkowych komentarzy:
            {{
            ""summary"": ""Podsumowanie artykułu..."",
            ""ankiCards"": [
                {{
                ""front"": ""Pytanie lub pojęcie"",
                ""back"": ""Odpowiedź z przykładem""
                }}]
            }}

            Treść artykułu:
            {content.Substring(0, Math.Min(content.Length, 8000))}";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("Jesteś ekspertem w tworzeniu materiałów edukacyjnych dla programistów. Zawsze odpowiadasz w formacie JSON."),
                new UserChatMessage(prompt)
            };

            var completion = await _chatClient.CompleteChatAsync(messages);
            var responseContent = completion.Value.Content[0].Text;

            _logger.LogInformation("OpenAI Response: {Response}", responseContent);

            // Usuń markdown code blocks jeśli są obecne
            var jsonContent = responseContent.Trim();
            if (jsonContent.StartsWith("```json"))
            {
                jsonContent = jsonContent.Substring(7); // Usuń ```json
            }
            else if (jsonContent.StartsWith("```"))
            {
                jsonContent = jsonContent.Substring(3); // Usuń ```
            }

            if (jsonContent.EndsWith("```"))
            {
                jsonContent = jsonContent.Substring(0, jsonContent.Length - 3); // Usuń końcowe ```
            }

            jsonContent = jsonContent.Trim();

            var result = JsonSerializer.Deserialize<ProcessUrlResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null || result.AnkiCards == null || !result.AnkiCards.Any())
            {
                throw new InvalidOperationException("Invalid response from OpenAI");
            }

            // Ogranicz do maksymalnie 3 fiszek
            if (result.AnkiCards.Count > 3)
            {
                result.AnkiCards = result.AnkiCards.Take(3).ToList();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Anki cards");
            throw;
        }
    }
}
