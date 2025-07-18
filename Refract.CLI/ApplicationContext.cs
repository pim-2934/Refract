namespace Refract.CLI;

public static class ApplicationContext
{
    public static string? SessionName { get; set; }
    public static string SessionsFolderPath => "sessions";
    public static string? ProjectFolderPath { get; set; }
    public static string? BinaryFilePath { get; set; }

    public static string CFilePath => $"{BinaryFilePath}.c";
    public static string DsmFilePath => $"{BinaryFilePath}.dsm";
    public static string AsmFilePath => $"{BinaryFilePath}.asm";
    public static string HexFilePath => $"{BinaryFilePath}.hex";
    public static string ChunkFolderPath => $"{ProjectFolderPath}/chunks";
}