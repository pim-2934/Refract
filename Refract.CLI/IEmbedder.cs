using Refract.CLI.Entities;
using Refract.CLI.Services.Embedders;

namespace Refract.CLI;

public interface IEmbedder
{
    Task<OllamaEmbedder.EmbeddingResponse> EmbedQuestionAsync(string question);
    Task EmbedChunksAsync(IEnumerable<Chunk> chunks);
}