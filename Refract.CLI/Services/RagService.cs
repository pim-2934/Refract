using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Refract.CLI.Services;

public class RagService(string vectorDbUrl, string embedderUrl, ILogger<RagService> logger)
{
    private readonly HttpClient _httpClient = new();
    private readonly OllamaService _ollamaService = new();

    public async Task<string> AskAsync(string question, string? sessionName)
    {
        var embedResp = await _httpClient.PostAsJsonAsync(
            embedderUrl,
            new
            {
                model = "nomic-embed-code",
                prompt = question
            }
        );
        
        embedResp.EnsureSuccessStatusCode();

        var embedData = await embedResp.Content.ReadFromJsonAsync<EmbeddingService.EmbeddingResponse>();
        if (embedData is null)
            throw new Exception("Question cannot be translated to embeddings.");
            
        var queryVector = embedData.Embedding;

        var qdrantReq = new
        {
            vector = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "text", queryVector.ToArray() }
            },
            limit = 100,
            with_payload = true
        };

        var queryResp = await _httpClient.PostAsJsonAsync(
            $"{vectorDbUrl}/collections/{sessionName}/points/query",
            qdrantReq
        );

        queryResp.EnsureSuccessStatusCode();
        var res = await queryResp.Content.ReadFromJsonAsync<QdrantQueryResponse>();

        if (res is null)
            throw new Exception("No relevant context found in the database.");
        
        foreach (var point in res.result.points)
        {
            logger.LogDebug("Retrieved chunk: {PayloadName} @ {PayloadAddress}",
                point.payload.name,
                point.payload.address
            );
        }

        var contexts = res.result.points
            .Select(p => p.payload.context)
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();

        if (contexts.Count == 0)
            throw new Exception("No relevant context found in the database.");

        return await _ollamaService.AskAsync(question, string.Join("\n", contexts));
    }

    private class QdrantQueryResponse
    {
        public ResultData result { get; set; }
        public string status { get; set; }
        public double time { get; set; }
    }

    private class ResultData
    {
        public List<QdrantPoint> points { get; set; }
    }

    private class QdrantPoint
    {
        public string id { get; set; }
        public int version { get; set; }
        public float score { get; set; }
        public Payload payload { get; set; }
    }

    private class Payload
    {
        public string text { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string context { get; set; }
    }
}