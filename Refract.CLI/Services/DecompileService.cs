using Microsoft.Extensions.Logging;

namespace Refract.CLI.Services;

public class DecompileService(ILogger<DecompileService> logger)
{
    public async Task DecompileAsync(string inputFilePath, string projectPath)
    {
        var fileInfo = new FileInfo(inputFilePath);
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments =
                $"/c docker compose run --rm disassembler retdec-decompiler.py --cleanup --mode bin --backend-no-debug --backend-no-debug-comments -o /{projectPath.Replace("\\", "/")}/{fileInfo.Name}.c /{inputFilePath.Replace("\\", "/")}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        // Read output asynchronously
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(output))
            logger.LogDebug(output);

        if (!string.IsNullOrEmpty(error))
            logger.LogError(error);

        logger.LogInformation("Decompilation process exited with code: {ProcessExitCode}", process.ExitCode);

        SplitDsmFile();
    }

    private void SplitDsmFile()
    {
        using var reader = new StreamReader(ApplicationContext.DsmFilePath);
        using var asmWriter = new StreamWriter(ApplicationContext.AsmFilePath);
        using var hexWriter = new StreamWriter(ApplicationContext.HexFilePath);

        var isHex = false;

        while (reader.ReadLine() is { } line)
        {
            if (line.Trim() == ";;" && reader.Peek() != -1)
            {
                var nextLine = reader.ReadLine()?.Trim() ?? "";
                if (nextLine == ";; Data Segment")
                {
                    reader.ReadLine();
                    isHex = true;
                }
                else
                {
                    if (!isHex) asmWriter.WriteLine(line);
                    if (!isHex) asmWriter.WriteLine(nextLine);
                }

                continue;
            }

            if (isHex)
                hexWriter.WriteLine(line);
            else
                asmWriter.WriteLine(line);
        }

        hexWriter.Flush();
        hexWriter.Close();
        asmWriter.Flush();
        asmWriter.Close();

        logger.LogInformation("Split complete: .asm and .hex created from .dsm file.");
    }
}