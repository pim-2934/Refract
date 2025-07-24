using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refract.CLI.Entities;

namespace Refract.CLI.Services.Indexers;

public class QdrantIndexer(IOptions<QdrantIndexer.Options> options, ILogger<QdrantIndexer> logger) : IIndexer
{
    private readonly HttpClient _httpClient = new() { Timeout = options.Value.Timeout };

    public async Task ProcessChunksAsync(Session session)
    {
        await _httpClient.PutAsJsonAsync($"{options.Value.Host}/collections/{session.Key}", new
            {
                vectors = new Dictionary<string, object>
                {
                    ["text"] = new
                    {
                        size = options.Value.VectorSpaceSize,
                        distance = options.Value.SimilarityDistance
                    }
                }
            }
        );

        foreach (var chunk in session.Chunks)
        {
            if (chunk.Embedding.Length == 0)
                logger.LogWarning("Chunk {ChunkId} has no embedding. Skipping.", chunk.Id);

            await UploadToVectorDbAsync(chunk, session.Key);
        }
    }

    public async Task<ResultData> Query(Session session, float[] embedding, Condition[] conditions)
    {
        var queryResp = await _httpClient.PostAsJsonAsync(
            $"{options.Value.Host}/collections/{session.Key}/points/query",
            new
            {
                vector = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase)
                {
                    { "text", embedding }
                },
                limit = 100,
                with_payload = true,
                filter = new
                {
                    must = conditions
                }
            }
        );

        queryResp.EnsureSuccessStatusCode();
        
        var res = await queryResp.Content.ReadFromJsonAsync<QueryResponse>();

        return res!.Result!;
    }

    public class Condition
    {
        [JsonPropertyName("key")] public required string Key { get; set; }

        [JsonPropertyName("match")] public required Match Match { get; set; }
    }

    public class Match
    {
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("any")] public string[]? Any { get; set; }
    }

    private class QueryResponse
    {
        [JsonPropertyName("result")] public ResultData? Result { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("time")] public double Time { get; set; }
    }
    
    public class ResultData
    {
        [JsonPropertyName("points")] public List<Point>? Points { get; set; }
    }

    public class Point
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("version")] public int Version { get; set; }
        [JsonPropertyName("score")] public float Score { get; set; }
        [JsonPropertyName("payload")] public required Payload Payload { get; set; }
    }

    public class Payload
    {
        [JsonPropertyName("contentType")] public required string ContentType { get; set; }
        [JsonPropertyName("content")] public required string Content { get; set; }
    }
    
    private async Task UploadToVectorDbAsync(Chunk chunk, string? sessionName)
    {
        var payload = new
        {
            points = new[]
            {
                new
                {
                    id = chunk.Id,
                    vector = new Dictionary<string, float[]>
                    {
                        { "text", chunk.Embedding }
                    },
                    payload = new
                    {
                        chunk.Id,
                        chunk.ContentType,
                        chunk.Content,
                        chunk.Meta
                    }
                }
            }
        };

        var response =
            await _httpClient.PutAsJsonAsync($"{options.Value.Host}/collections/{sessionName}/points", payload);

        response.EnsureSuccessStatusCode();
    }

    public class Options
    {
        public string Host { get; set; } = "http://localhost:6333";
        public int VectorSpaceSize { get; set; } = 3584;
        public string SimilarityDistance { get; set; } = "Cosine";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }
}