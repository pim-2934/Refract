namespace Refract.CLI.Entities;

public class Chunk
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public required string ContentType { get; init; }
    public required Metadata Meta { get; init; }
    public float[] Embedding { get; set; } = [];

    public class Metadata
    {
        public required int ChunkIndex { get; init; }
        public required int StartLine { get; init; }
        public required int EndLine { get; init; }
    }
}