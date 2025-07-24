using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Refract.CLI.Services;

public class VectorDbService
{
    private readonly string _vectorDbUrl;
    private readonly HttpClient _httpClient;

    public VectorDbService(string vectorDbUrl, ILogger<VectorDbService> logger)
    {
        _vectorDbUrl = vectorDbUrl.TrimEnd('/');
        _httpClient = new HttpClient();
    }

    private async Task UploadToVectorDbAsync(Chunk chunk, string? sessionName)
    {
        var qdrantUrl = $"{_vectorDbUrl}/collections/{sessionName}/points";

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
                        Context = chunk.Content,
                        chunk.Meta
                    }
                }
            }
        };

        var response = await _httpClient.PutAsJsonAsync(qdrantUrl, payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task ProcessChunksAsync(Session session)
    {
        await _httpClient.PutAsJsonAsync($"{_vectorDbUrl}/collections/{session.Key}", new
            {
                vectors = new Dictionary<string, object>
                {
                    ["text"] = new
                    {
                        size = 3584,
                        distance = "Cosine"
                    }
                }
            }
        );

        foreach (var chunk in session.Chunks)
        {
            await UploadToVectorDbAsync(chunk, session.Key);
        }
    }
}