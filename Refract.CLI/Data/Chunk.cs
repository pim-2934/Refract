namespace Refract.CLI;

public class Chunk
{
    public string id { get; set; }
    public string name { get; set; }
    public string address { get; set; }
    public string context { get; set; }
    public float[] embedding { get; set; }
}