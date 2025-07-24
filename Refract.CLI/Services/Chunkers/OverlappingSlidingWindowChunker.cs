using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Refract.CLI.Entities;

namespace Refract.CLI.Services.Chunkers;

public class OverlappingSlidingWindowChunker(IOptions<OverlappingSlidingWindowChunker.Options> options) : IChunker
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

                if (estimatedTokens + tokensInLine > options.Value.TargetTokenEstimate)
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
                Content = chunkText,
                Meta = new Chunk.Metadata
                {
                    ChunkIndex = i,
                    StartLine = i,
                    EndLine = j - 1
                }
            });

            // Move index forward by window size minus overlap
            i = j - options.Value.OverlapLines;
            if (i <= j - chunkLines.Count) i = j;
        }

        return chunks;
    }

    private static int EstimateTokens(string line)
    {
        // 3.5 chars per token is a good middle-ground estimate for code.. maybe..
        return Math.Max(1, line.Length / 3);
    }

    public class Options
    {
        /// <summary>
        /// Represents the target token estimate used by the chunker mechanism in the application.
        /// The value is set slightly lower than the maximum capacity of the embedder to allow for overlap,
        /// enabling better handling of grouped data units.
        /// </summary>
        /// <remarks>
        /// This value is configurable and should be adjusted based on the embedder's requirements
        /// and batch size considerations. By default, it is set to 256.
        /// </remarks>
        public int TargetTokenEstimate { get; set; } = 256;
        
        /// <summary>
        /// Indicates the number of overlapping lines used by the chunker to provide context
        /// when splitting data into manageable segments.
        /// This helps preserve continuity between segments, especially for related or dependent data.
        /// </summary>
        /// <remarks>
        /// The overlap is measured in lines and allows for smoother transitions between chunks.
        /// The default value is set to 2, ensuring minimal yet sufficient overlap for maintaining context.
        /// This value can be adjusted based on specific application needs or data handling requirements.
        /// </remarks>
        public int OverlapLines { get; set; } = 2;
    }
}