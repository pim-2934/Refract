using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Refract.CLI.Data;

namespace Refract.CLI.Services;

public class EmbeddingService(string embedderUrl, ILogger<EmbeddingService> logger)
{
    private readonly HttpClient _httpClient = new();
    private ILogger<EmbeddingService> _logger = logger;

    public async Task<List<Chunk>> EmbedChunksAsync(List<Chunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            await EmbedChunkAsync(chunk);
        }

        return chunks;
    }
    
    private async Task EmbedChunkAsync(Chunk chunk)
    {
        var embedResp = await _httpClient.PostAsJsonAsync(
            embedderUrl,
            new { inputs = new[] { $"passage: {chunk.Content}" } }
        );

        embedResp.EnsureSuccessStatusCode();

        var embedData = await embedResp.Content.ReadFromJsonAsync<JsonElement>();

        switch (embedData.ValueKind)
        {
            case JsonValueKind.Array:
            {
                var embedding = embedData[0].EnumerateArray().Select(x => x.GetSingle()).ToArray();
                chunk.Embedding = embedding;
                return;
            }
            case JsonValueKind.Object when embedData.TryGetProperty("error", out var errorMsg) &&
                                           embedData.TryGetProperty("error_type", out var errorType):
                _logger.LogError("Embedding service error: {GetString} - {S}", errorType.GetString(),
                    errorMsg.GetString());
                return;
            case JsonValueKind.Undefined:
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            default:
                throw new InvalidOperationException($"Unexpected response type: {embedData.ValueKind}");
        }
    }
}