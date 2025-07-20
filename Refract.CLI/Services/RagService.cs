using System.Net.Http.Json;
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
            filter = BuildFilter
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

    private List<Filter> BuildFilter(string question)
    {
        var contentTypes = new List<string> { "C" };

        var lowerQuestion = question.ToLowerInvariant();

        if (AssemblyCodeRequestedRegex().IsMatch(lowerQuestion))
            contentTypes.Add("ASM");

        if (HexDataRequestedRegex().IsMatch(lowerQuestion))
            contentTypes.Add("HEX");

        return
        [
            new Filter
            {
                Must =
                [
                    new Condition
                    {
                        Key = "ContentType",
                        MatchAny = new MatchAny { Values = contentTypes }
                    }
                ]
            }
        ];
    }

    [GeneratedRegex(@"\b(hex|bytes|raw data|memory dump)\b")]
    private static partial Regex HexDataRequestedRegex();
    
    [GeneratedRegex(@"\b(asm|assembly|disassembly|opcode|instruction)\b")]
    private static partial Regex AssemblyCodeRequestedRegex();
    
    private class Filter
    {
        public List<Condition> Must { get; set; }
    }

    private class Condition
    {
        public required string Key { get; set; }
        public required MatchAny MatchAny { get; set; }
    }

    private class MatchAny
    {
        public required List<string> Values { get; set; }
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