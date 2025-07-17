using System.Net.Http.Json;
using System.Text.Json;

namespace Refract.CLI;

public class EmbeddingService
{
    private readonly string _embedderUrl;
    private readonly HttpClient _httpClient;

    public EmbeddingService(string embedderUrl, HttpClient httpClient = null)
    {
        _embedderUrl = embedderUrl;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task EmbedChunkAsync(Chunk chunk)
    {
        var embedResp = await _httpClient.PostAsJsonAsync(
            _embedderUrl,
            new { inputs = new[] { $"passage: {chunk.context}" } }
        );

        // Check if the response is successful
        embedResp.EnsureSuccessStatusCode();

        var embedData = await embedResp.Content.ReadFromJsonAsync<JsonElement>();

        // Check the response structure
        if (embedData.ValueKind == JsonValueKind.Array)
        {
            // Success case: Response is an array of embeddings
            var embedding = embedData[0].EnumerateArray().Select(x => x.GetSingle()).ToArray();
            chunk.embedding = embedding;
            return;
        }

        if (embedData.ValueKind == JsonValueKind.Object)
        {
            // Error case: Response is an object with error details
            if (embedData.TryGetProperty("error", out var errorMsg) &&
                embedData.TryGetProperty("error_type", out var errorType))
            {
                Console.WriteLine($"Embedding service error: {errorType.GetString()} - {errorMsg.GetString()}");
                return;
            }

            throw new InvalidOperationException("Unexpected response format from embedding service");
        }

        throw new InvalidOperationException($"Unexpected response type: {embedData.ValueKind}");
    }

    public async Task<List<Chunk>> EmbedChunksAsync(List<Chunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            await EmbedChunkAsync(chunk);
        }

        return chunks;
    }
}