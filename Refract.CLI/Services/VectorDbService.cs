using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Refract.CLI.Data;

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

    public async Task SaveChunkLocallyAsync(Chunk chunk, string chunksFolder)
    {
        Directory.CreateDirectory(chunksFolder);

        var chunkPath = Path.Combine(chunksFolder, $"{chunk.Id}.json");
        await File.WriteAllTextAsync(chunkPath,
            JsonSerializer.Serialize(chunk, new JsonSerializerOptions { WriteIndented = true }));
    }

    public async Task UploadToVectorDbAsync(Chunk chunk, string? sessionName)
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
                        chunk.Context,
                        chunk.Metadata
                    }
                }
            }
        };

        var response = await _httpClient.PutAsJsonAsync(qdrantUrl, payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task ProcessChunksAsync(List<Chunk> chunks, string chunksFolder, string? sessionName)
    {
        await _httpClient.PutAsJsonAsync($"{_vectorDbUrl}/collections/{sessionName}", new
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

        foreach (var chunk in chunks)
        {
            await SaveChunkLocallyAsync(chunk, chunksFolder);
            await UploadToVectorDbAsync(chunk, sessionName);
        }
    }
}