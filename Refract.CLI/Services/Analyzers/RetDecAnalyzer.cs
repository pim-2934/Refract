using Microsoft.Extensions.Logging;
using Refract.CLI.Entities;

namespace Refract.CLI.Services.Analyzers;

public class RetDecAnalyzer(ILogger<RetDecAnalyzer> logger) : IBinaryAnalyzer
{
    public async Task<string> DecompileToCAsync(Session session)
    {
        if (!File.Exists($"{session.BinaryFilePath}.c"))
            await DecompileAsync(session);

        return await File.ReadAllTextAsync($"{session.BinaryFilePath}.c");
    }

    public async Task<string> DisassembleToAsmAsync(Session session)
    {
        if (!File.Exists($"{session.BinaryFilePath}.asm"))
            await DecompileAsync(session);

        return await File.ReadAllTextAsync($"{session.BinaryFilePath}.asm");
    }

    public async Task<string> DumpHexAsync(Session session)
    {
        if (!File.Exists($"{session.BinaryFilePath}.hex"))
            await DecompileAsync(session);

        return await File.ReadAllTextAsync($"{session.BinaryFilePath}.hex");
    }

    private async Task DecompileAsync(Session session)
    {
        var binaryFilePath = session.BinaryFilePath.Replace("\\", "/");
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments =
                $"/c docker compose run --rm analyzer retdec-decompiler.py --cleanup --mode bin --backend-no-debug --backend-no-debug-comments -o /{binaryFilePath}.c /{binaryFilePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(output))
            logger.LogDebug(output);

        if (!string.IsNullOrEmpty(error))
            logger.LogError(error);

        logger.LogInformation("Decompilation process exited with code: {ProcessExitCode}", process.ExitCode);

        SplitDsmFile(session);
    }

    private void SplitDsmFile(Session session)
    {
        using var reader = new StreamReader($"{session.BinaryFilePath}.dsm");
        using var asmWriter = new StreamWriter($"{session.BinaryFilePath}.asm");
        using var hexWriter = new StreamWriter($"{session.BinaryFilePath}.hex");

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

        // TODO: cleanup the HEX output

        hexWriter.Flush();
        hexWriter.Close();
        asmWriter.Flush();
        asmWriter.Close();

        logger.LogInformation("Split complete: .asm and .hex created from .dsm file.");
    }
}