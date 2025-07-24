using Refract.CLI.Entities;
using Refract.CLI.Services.Indexers;

namespace Refract.CLI;

public interface IIndexer
{
    Task ProcessChunksAsync(Session session);
    Task<QdrantIndexer.ResultData> Query(Session session, float[] embedding, QdrantIndexer.Condition[] conditions); // TODO: make implementation independent
}