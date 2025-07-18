namespace Refract.CLI.Data;

public class Chunk
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required string Context { get; set; }
    public float[] Embedding { get; set; } = [];
}