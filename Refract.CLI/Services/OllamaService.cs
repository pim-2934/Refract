using System.Net.Http.Json;
using System.Text.Json;

namespace Refract.CLI.Services;

public class OllamaService(string ollamaUrl = "http://localhost:11435/api/generate")
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> AskAsync(string question, string context)
    {
        var prompt = $"""
                      You are an intelligent and direct reverse engineering assistant. Always use the provided context to answer. Never make up information not in the context. Respond insightful and straight to the point.

                      Context:
                      {context}

                      Question:
                      {question}

                      Answer:
                      """;

        var payload = new
        {
            model = "mistral",
            prompt = prompt,
            stream = false,
            temperature = 0.2,
            top_p = 0.9,
            repeat_penalty = 1.2
        };

        var response = await _httpClient.PostAsJsonAsync(ollamaUrl, payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Ollama request failed: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("response").GetString() ?? "";
    }
}