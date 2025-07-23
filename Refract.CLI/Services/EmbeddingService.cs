using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Refract.CLI.Services;

public class EmbeddingService(string embedderUrl, ILogger<EmbeddingService> logger)
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(10) };
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
            new
            {
                model = "nomic-embed-code",
                prompt = chunk.Context
            }
        );

        embedResp.EnsureSuccessStatusCode();

        var embedData = await embedResp.Content.ReadFromJsonAsync<EmbeddingResponse>();

        if (embedData is null)
            throw new Exception($"Error embedding chunk {chunk.Id}.");

        chunk.Embedding = embedData.Embedding.ToArray();
    }

    public class EmbeddingResponse
    {
        public required List<float> Embedding { get; init; }
    }
}