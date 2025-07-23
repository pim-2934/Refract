namespace Refract.CLI;

public static class ApplicationContext
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
    public static int ChunkerTargetTokenEstimate { get; set; } = 256;

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
    public static int ChunkerOverlapLines { get; set; } = 2;

    public static string SessionsFolderPath => "sessions";
}