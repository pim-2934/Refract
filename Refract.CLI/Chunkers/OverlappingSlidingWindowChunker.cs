using System.Security.Cryptography;
using System.Text;
using Refract.CLI.Data;

namespace Refract.CLI.Chunkers;

public class OverlappingSlidingWindowChunker(int targetTokenEstimate, int overlapLines) : IChunker
{
    public List<Chunk> CreateChunks(string data, string contentType)
    {
        var lines = data.Split('\n');
        var chunks = new List<Chunk>();

        var i = 0;
        while (i < lines.Length)
        {
            var chunkLines = new List<string>();
            var estimatedTokens = 0;
            var j = i;

            while (j < lines.Length)
            {
                var line = lines[j];
                var tokensInLine = EstimateTokens(line);

                if (estimatedTokens + tokensInLine > targetTokenEstimate)
                    break;

                chunkLines.Add(line);
                estimatedTokens += tokensInLine;
                j++;
            }

            var uid = BitConverter
                .ToString(MD5.HashData(Encoding.UTF8.GetBytes($"{contentType}_{i}")))
                .Replace("-", "")
                .ToLower();

            var chunkText = string.Join('\n', chunkLines);
            chunks.Add(new Chunk
            {
                Id = uid,
                ContentType = contentType,
                Context = chunkText,
                Metadata = new Metadata
                {
                    ChunkIndex = i,
                    StartLine = i,
                    EndLine = j - 1
                }
            });

            // Move index forward by window size minus overlap
            i = j - overlapLines;
            if (i <= j - chunkLines.Count) i = j;
        }

        return chunks;
    }

    private static int EstimateTokens(string line)
    {
        // 3.5 chars per token is a good middle-ground estimate for code.. maybe..
        return Math.Max(1, line.Length / 3);
    }
}