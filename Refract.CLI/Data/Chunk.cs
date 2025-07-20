namespace Refract.CLI.Data;

public class Chunk
{
    public required string Id { get; init; }
    public required string Context { get; init; }
    public required string ContentType { get; init; }
    public required Metadata Metadata { get; init; }
    public float[] Embedding { get; set; } = [];
}