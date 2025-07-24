using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refract.CLI.Entities;

namespace Refract.CLI.Services.Embedders;

public class OllamaEmbedder(IOptions<OllamaEmbedder.Options> options, ILogger<OllamaEmbedder> logger) : IEmbedder
{
    private readonly string _embedderUrl = $"{options.Value.Host}/api/embeddings";
    private readonly HttpClient _httpClient = new() { Timeout = options.Value.Timeout };

    public async Task<EmbeddingResponse> EmbedQuestionAsync(string question)
    {
        var embedResp = await _httpClient.PostAsJsonAsync(
            _embedderUrl,
            new
            {
                model = options.Value.Model,
                prompt = question
            }
        );

        embedResp.EnsureSuccessStatusCode();

        var embedData = await embedResp.Content.ReadFromJsonAsync<EmbeddingResponse>();
        if (embedData is null)
            throw new Exception("Question cannot be translated to embeddings.");

        return embedData;
    }

    public async Task EmbedChunksAsync(IEnumerable<Chunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            await EmbedChunkAsync(chunk);
        }
    }

    private async Task EmbedChunkAsync(Chunk chunk)
    {
        var embedResp = await _httpClient.PostAsJsonAsync(
            _embedderUrl,
            new EmbeddingRequest
            {
                Model = options.Value.Model,
                Prompt = chunk.Content
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

    public class EmbeddingRequest
    {
        [JsonPropertyName("model")] public required string Model { get; init; }
        [JsonPropertyName("prompt")] public required string Prompt { get; init; }
    }

    public class Options
    {
        public string Host { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "nomic-embed-code";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}