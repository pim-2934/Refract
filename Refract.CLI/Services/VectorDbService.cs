using System.Net.Http.Json;
using System.Text.Json;

namespace Refract.CLI;

public class VectorDbService
{
    private readonly string _collectionName = "refract"; // You can make this configurable if needed
    private readonly string _vectorDbUrl;
    private readonly HttpClient _httpClient;
    private readonly string _chunkOutputPath;

    public VectorDbService(string vectorDbUrl, string chunkOutputPath, HttpClient httpClient = null)
    {
        _vectorDbUrl = vectorDbUrl.TrimEnd('/');
        _chunkOutputPath = chunkOutputPath;
        _httpClient = httpClient ?? new HttpClient();

        Directory.CreateDirectory(_chunkOutputPath);
    }

    public async Task SaveChunkLocallyAsync(Chunk chunk)
    {
        var chunkPath = Path.Combine(_chunkOutputPath, $"{chunk.id}.json");
        await File.WriteAllTextAsync(chunkPath,
            JsonSerializer.Serialize(chunk, new JsonSerializerOptions { WriteIndented = true }));
    }

    public async Task UploadToVectorDbAsync(Chunk chunk)
    {
        var qdrantUrl = $"{_vectorDbUrl}/collections/{_collectionName}/points";

        var payload = new
        {
            points = new[]
            {
                new
                {
                    id = chunk.id,
                    vector = new Dictionary<string, float[]>
                    {
                        { "text", chunk.embedding }
                    },
                    payload = new
                    {
                        name = chunk.name,
                        address = chunk.address,
                        context = chunk.context
                    }
                }
            }
        };

        var response = await _httpClient.PutAsJsonAsync(qdrantUrl, payload);
        var foo = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
    }

    public async Task ProcessChunksAsync(List<Chunk> chunks)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_vectorDbUrl}/collections/{_collectionName}", new

        {
            vectors = new Dictionary<string, object>
            {
                ["text"] = new
                {
                    size = 1024,
                    distance = "Cosine"
                }
            }
        }
        );

        foreach (var chunk in chunks)
        {
            await SaveChunkLocallyAsync(chunk);
            await UploadToVectorDbAsync(chunk);
        }
    }
}