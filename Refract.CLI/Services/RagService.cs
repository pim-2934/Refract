using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Refract.CLI.Entities;
using Refract.CLI.Services.Indexers;

namespace Refract.CLI.Services;

public partial class RagService(IEmbedder embedder, IIndexer indexer, ITalker talker, ILogger<RagService> logger)
{
    public async Task<string> AskAsync(Session session, string question)
    {
        var embedData = await embedder.EmbedQuestionAsync(question);
        var queryVector = embedData.Embedding;
        var result = await indexer.Query(session, queryVector.ToArray(), BuildFilter(question).ToArray());

        if (result.Points is null || result.Points.Count == 0)
            throw new Exception("No relevant context found in the database.");

        foreach (var point in result.Points)
        {
            logger.LogDebug("Retrieved chunk: {PayloadName}", point.Id);
        }

        var contents = result.Points
            .Select(p => p.Payload.Content)
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();

        if (contents.Count == 0)
            throw new Exception("No relevant context found in the database.");

        return await talker.AskAsync(question, string.Join("\n", contents));
    }

    private static List<QdrantIndexer.Condition> BuildFilter(string question)
    {
        var contentTypes = new List<string> { "C" };
        var lowerQuestion = question.ToLowerInvariant();

        if (AssemblyCodeRequestedRegex().IsMatch(lowerQuestion))
            contentTypes.Add("ASM");

        if (HexDataRequestedRegex().IsMatch(lowerQuestion))
            contentTypes.Add("HEX");

        return
        [
            new QdrantIndexer.Condition
            {
                Key = "contentType",
                Match = new QdrantIndexer.Match { Any = contentTypes.ToArray() }
            }
        ];
    }

    [GeneratedRegex(@"\b(hex|bytes|raw data|memory dump)\b")]
    private static partial Regex HexDataRequestedRegex();

    [GeneratedRegex(@"\b(asm|assembly|disassembly|opcode|instruction)\b")]
    private static partial Regex AssemblyCodeRequestedRegex();
}