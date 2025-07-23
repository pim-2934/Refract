using System.Text.Json;
using Refract.CLI.Utilities;

namespace Refract.CLI;

public class Session
{
    public List<Chunk> Chunks { get; } = [];

    public string Key { get; private set; }

    public string SessionPath { get; }
    public string ChunksPath { get; }
    public string BinaryFilePath { get; }

    public string CFilePath => $"{BinaryFilePath}.c";
    public string DsmFilePath => $"{BinaryFilePath}.dsm";
    public string AsmFilePath => $"{BinaryFilePath}.asm";
    public string HexFilePath => $"{BinaryFilePath}.hex";

    public static async Task<Session> InitAsync(string targetBinaryPath)
    {
        var fileBytes = await File.ReadAllBytesAsync(targetBinaryPath);
        var crc32 = HashUtilities.CalculateCrc32(fileBytes);

        var session = new Session(crc32.ToString("X8"));

        if (!Directory.Exists(session.SessionPath))
            Directory.CreateDirectory(session.SessionPath);

        File.Copy(targetBinaryPath, session.BinaryFilePath, true);

        session.LoadChunks();

        return session;
    }

    public void Save()
    {
        SaveChunks();
    }

    public void SetChunks(List<Chunk> chunks)
    {
        Chunks.Clear();
        Chunks.AddRange(chunks);
    }

    private Session(string key)
    {
        Key = key;
        SessionPath = Path.Combine(ApplicationContext.SessionsFolderPath, key);
        ChunksPath = Path.Combine(SessionPath, "chunks");
        BinaryFilePath = Path.Combine(SessionPath, key);
    }

    private void SaveChunks()
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

    private void LoadChunks()
    {
        Chunks.Clear();

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
                    Chunks.Add(chunk);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading chunk from {chunkFile}: {ex.Message}");
            }
        }
    }
}