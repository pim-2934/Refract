namespace Refract.CLI;

public interface IChunker
{
    public List<Chunk> CreateChunks(string data, string contentType);
}