using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Refract.CLI.Services;

public partial class RagService(string vectorDbUrl, string embedderUrl, ILogger<RagService> logger)
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
            with_payload = true,
            filter = new
            {
                must = BuildFilter(question)
            }
        };

        var queryResp = await _httpClient.PostAsJsonAsync(
            $"{vectorDbUrl}/collections/{sessionName}/points/query",
            qdrantReq
        );

        queryResp.EnsureSuccessStatusCode();
        var res = await queryResp.Content.ReadFromJsonAsync<QdrantQueryResponse>();

        if (res?.Result?.Points is null || res.Result.Points.Count == 0)
            throw new Exception("No relevant context found in the database.");

        foreach (var point in res.Result.Points)
        {
            logger.LogDebug("Retrieved chunk: {PayloadName}", point.Id);
        }

        var contexts = res.Result.Points
            .Select(p => p.Payload.Context)
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();

        if (contexts.Count == 0)
            throw new Exception("No relevant context found in the database.");

        return await _ollamaService.AskAsync(question, string.Join("\n", contexts));
    }

    private List<Condition> BuildFilter(string question)
    {
        var contentTypes = new List<string> { "C" };

        var lowerQuestion = question.ToLowerInvariant();

        if (AssemblyCodeRequestedRegex().IsMatch(lowerQuestion))
            contentTypes.Add("ASM");

        if (HexDataRequestedRegex().IsMatch(lowerQuestion))
            contentTypes.Add("HEX");

        return
        [
            new Condition
            {
                Key = "contentType",
                Match = new Match { Any = contentTypes.ToArray() }
            }
        ];
    }

    [GeneratedRegex(@"\b(hex|bytes|raw data|memory dump)\b")]
    private static partial Regex HexDataRequestedRegex();

    [GeneratedRegex(@"\b(asm|assembly|disassembly|opcode|instruction)\b")]
    private static partial Regex AssemblyCodeRequestedRegex();

    private class Condition
    {
        [JsonPropertyName("key")] public required string Key { get; set; }

        [JsonPropertyName("match")] public required Match Match { get; set; }
    }

    private class Match
    {
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("any")] public string[]? Any { get; set; }
    }

    private class QdrantQueryResponse
    {
        [JsonPropertyName("result")] public ResultData? Result { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("time")] public double Time { get; set; }
    }

    private class ResultData
    {
        [JsonPropertyName("points")] public List<QdrantPoint>? Points { get; set; }
    }

    private class QdrantPoint
    {
        [JsonPropertyName("id")] public string? Id { get; set; }

        [JsonPropertyName("version")] public int Version { get; set; }

        [JsonPropertyName("score")] public float Score { get; set; }
        [JsonPropertyName("payload")] public required Payload Payload { get; set; }
    }

    private class Payload
    {
        [JsonPropertyName("contentType")] public required string ContentType { get; set; }
        [JsonPropertyName("context")] public required string Context { get; set; }
    }
}