using System.Text.Json;

namespace Refract.CLI.Entities;

public class Session
{
    public IReadOnlyList<Chunk> Chunks { get; private set; } = [];
    public string Key { get; private set; }
    public string ChunksPath { get; }
    public string BinaryFilePath { get; }
    public string SessionPath { get; }

    public Session(string key, string sessionsFolderPath)
    {
        Key = key;
        SessionPath = Path.Combine(sessionsFolderPath, key);
        ChunksPath = Path.Combine(SessionPath, "chunks");
        BinaryFilePath = Path.Combine(SessionPath, key);
    }

    public void Save()
    {
        if (!Directory.Exists(ChunksPath))
            Directory.CreateDirectory(ChunksPath);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        foreach (var chunk in Chunks)
        {
            var filePath = Path.Combine(ChunksPath, $"{chunk.Id}.json");

            try
            {
                var jsonContent = JsonSerializer.Serialize(chunk, options);
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving chunk {chunk.Id}: {ex.Message}");
            }
        }
    }

    public void Load()
    {
        var chunks = new List<Chunk>();

        if (!Directory.Exists(ChunksPath)) return;

        var chunkFiles = Directory.GetFiles(ChunksPath, "*.json", SearchOption.TopDirectoryOnly);

        foreach (var chunkFile in chunkFiles)
        {
            try
            {
                var jsonContent = File.ReadAllText(chunkFile);
                var chunk = JsonSerializer.Deserialize<Chunk>(jsonContent);

                if (chunk != null)
                {
                    chunks.Add(chunk);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading chunk from {chunkFile}: {ex.Message}");
            }
        }

        Chunks = chunks;
    }

    public void SetChunks(List<Chunk> chunks)
    {
        Chunks = chunks;
    }
}