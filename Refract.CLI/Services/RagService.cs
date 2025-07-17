using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Refract.CLI
{
    public class RagService
    {
        private readonly HttpClient _httpClient;
        private readonly string _vectorDbUrl;
        private readonly string _embedderUrl;
        private readonly string _collectionName = "refract"; // make sure this matches your upsert target
        private readonly OllamaService _ollamaService;
        private readonly string ollamaUrl = "http://localhost:11434/api/generate";
        private readonly ILogger<RagService> _logger;

        public RagService(string vectorDbUrl, string embedderUrl, ILogger<RagService> logger)
        {
            _vectorDbUrl = vectorDbUrl;
            _embedderUrl = embedderUrl;
            _httpClient = new HttpClient();
            _ollamaService = new OllamaService(ollamaUrl);
            _logger = logger;
        }

        public async Task<string> AskAsync(string question)
        {
            // 1️⃣ Embed the user's question
            var embedResp = await _httpClient.PostAsJsonAsync(
                $"{_embedderUrl}",
                new { inputs = new[] { $"query: {question}" } }
            );
            embedResp.EnsureSuccessStatusCode();
            var embedData = await embedResp.Content.ReadFromJsonAsync<List<List<float>>>();
            var queryVector = embedData[0];

            // 2️⃣ Query Qdrant using correct field name "query"
            var qdrantReq = new
            {
                vector = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase)
                {
                    { "text", queryVector.ToArray() }
                },
                limit = 20,
                with_payload = true
            };

            var queryResp = await _httpClient.PostAsJsonAsync(
                $"{_vectorDbUrl}/collections/{_collectionName}/points/query",
                qdrantReq
            );

            queryResp.EnsureSuccessStatusCode();
            var res = await queryResp.Content.ReadFromJsonAsync<QdrantQueryResponse>();

            foreach (var point in res.result.points)
            {
                _logger.LogDebug("Retrieved chunk: {PayloadName} @ {PayloadAddress}",
                    point.payload.name,
                    point.payload.address
                );
            }

            // 3️⃣ Extract and concatenate chunk texts
            var contexts = res.result.points
                .Select(p => p.payload.context)
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            if (contexts.Count == 0)
                Console.WriteLine("❌ No relevant context found in the database.");

            return await _ollamaService.AskAsync(question, string.Join("\n", contexts));
        }

        private class EmbeddingResult
        {
            public List<List<float>> embeddings { get; set; }
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


        private class QdrantSearchResponse
        {
            public List<QdrantPoint> result { get; set; }
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
}