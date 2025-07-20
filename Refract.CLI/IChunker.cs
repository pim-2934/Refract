using System.Text.Json;
using Refract.CLI.Data;

namespace Refract.CLI;

public interface IChunker
{
    public List<Chunk> CreateChunks(string data, string contentType);

    public void SaveChunks(List<Chunk> chunks, string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        foreach (var chunk in chunks)
        {
            var filePath = Path.Combine(directoryPath, $"{chunk.Id}.json");

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

    public List<Chunk> LoadChunks(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        var chunks = new List<Chunk>();
        var chunkFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);

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

        return chunks;
    }
}