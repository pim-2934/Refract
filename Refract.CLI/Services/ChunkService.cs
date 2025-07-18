using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClangSharp;
using ClangSharp.Interop;
using Refract.CLI.Data;

namespace Refract.CLI.Services;

/**
 * TODO: write different chunker implementations for different targets
 * - Sliding Window Chunking (with overlap) for Hex (can be the default I think)
 * - C Syntax aware scope chunking (Brace matching?), one chunk per block (if, for, switch, etc)
 * - N instructions w/ overlap for ASM (aligned with call, jmp, ret)
 */
public static class ChunkService
{
    public static List<Chunk> CreateChunksFromCFile(string cFilePath, string asmFilePath)
    {
        var chunks = new List<Chunk>();
        var cLines = File.ReadAllLines(cFilePath);
        var asmLines = File.Exists(asmFilePath) ? File.ReadAllLines(asmFilePath) : [];

        using var index = CXIndex.Create();
        var tu = CXTranslationUnit.Parse(index, cFilePath, [], [], CXTranslationUnit_Flags.CXTranslationUnit_None);
        var translationUnit = TranslationUnit.GetOrCreate(tu);
        var cursor = translationUnit.TranslationUnitDecl;

        foreach (var func in cursor.CursorChildren.OfType<FunctionDecl>())
        {
            if (!func.HasBody) // This is just a prototype/declaration, skip it
                continue;

            var extent = func.Extent;
            extent.Start.GetFileLocation(out _, out var start, out _, out _);
            extent.End.GetFileLocation(out _, out var end, out _, out _);

            var name = func.Name;
            var cCode = string.Join("\n", cLines.Skip((int)start - 1).Take((int)(end - start + 1)));

            var address = "0x000000";
            var functionDeclRegex = new Regex($"; function: ({Regex.Escape(name)}) at (0x[a-fA-F0-9]+)");
            foreach (var line in asmLines)
            {
                var match = functionDeclRegex.Match(line);

                if (!match.Success) continue;

                address = match.Groups[2].Value;
                break;
            }

            var asm = ExtractAssemblyFunction(asmLines, name, address);

            var uid = BitConverter
                .ToString(MD5.HashData(Encoding.UTF8.GetBytes(name)))
                .Replace("-", "")
                .ToLower();

            chunks.Add(new Chunk
            {
                Id = $"{uid}",
                Name = name,
                Address = address,
                Context = $"""
                           Type: C
                           Function: {name}
                           Address: {address}
                           Signature: {func.Type}

                           [C]
                           {cCode}
                           """
            });

            // ASM chunks to big!            
            //             chunks.Add(new Chunk
            //             {
            //                 Id = $"{uid}-asm",
            //                 Name = name,
            //                 Address = address,
            //                 Context = $"""
            //                            Type: ASM
            //                            Function: {name}
            //                            Address: {address}
            //                            Signature: {func.Type}
            //
            //                            [ASM]
            //                            {asm}
            //                            """
            //             });
        }

        return chunks;
    }

    public static List<Chunk> LoadChunksFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        var chunks = new List<Chunk>();
        var chunkFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);

        foreach (var chunkFile in chunkFiles)
        {
            try
            {
                var jsonContent = File.ReadAllText(chunkFile);
                var chunk = JsonSerializer.Deserialize<Chunk>(jsonContent);

                if (chunk != null)
                {
                    chunks.Add(chunk);
                }
            }
            catch (Exception ex)
            {
                // Skip invalid files
                Console.WriteLine($"Error loading chunk from {chunkFile}: {ex.Message}");
            }
        }

        return chunks;
    }

    public static void SaveChunks(List<Chunk> chunks, string directoryPath)
    {
        if (chunks == null || !chunks.Any())
        {
            return;
        }

        // Create directory if it doesn't exist
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        foreach (var chunk in chunks)
        {
            if (string.IsNullOrEmpty(chunk.Id))
            {
                continue; // Skip chunks without an ID
            }

            var filePath = Path.Combine(directoryPath, $"{chunk.Id}.json");

            try
            {
                var jsonContent = JsonSerializer.Serialize(chunk, options);
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving chunk {chunk.Id}: {ex.Message}");
            }
        }
    }


    private static string ExtractAssemblyFunction(string[] asmLines, string functionName, string functionAddress)
    {
        var functionStartIndex = -1;
        var functionEndAddress = "";

        var functionDeclPattern = $"; function: {Regex.Escape(functionName)} at {Regex.Escape(functionAddress)}";

        for (var i = 0; i < asmLines.Length; i++)
        {
            if (!asmLines[i].Contains(functionDeclPattern)) continue;
            functionStartIndex = i;

            var endAddressMatch = Regex.Match(asmLines[i], @"-- (0x[a-fA-F0-9]+)");
            if (endAddressMatch.Success)
            {
                functionEndAddress = endAddressMatch.Groups[1].Value;
            }

            break;
        }

        if (functionStartIndex == -1)
        {
            return string.Join(
                "\n",
                asmLines.Where(line => line.Contains(functionName)
                    )
                    .Select(RemoveComments));
        }

        var asmCode = new List<string> { asmLines[functionStartIndex] };

        for (var i = functionStartIndex + 1; i < asmLines.Length; i++)
        {
            var line = asmLines[i];

            if (line.Contains("; function:") && !line.Contains(functionName))
            {
                break;
            }

            if (line.TrimStart().StartsWith("; section:"))
            {
                continue;
            }

            if (line.TrimStart().StartsWith("; data inside code section"))
            {
                continue;
            }

            asmCode.Add(RemoveComments(line));

            if (!string.IsNullOrEmpty(functionEndAddress) && line.TrimStart().StartsWith(functionEndAddress + ":"))
            {
                break;
            }
        }

        return string.Join("\n", asmCode);
    }

    private static string RemoveComments(string asmLine)
    {
        if (asmLine.TrimStart().StartsWith("; function:"))
            return asmLine;

        var commentIndex = asmLine.IndexOf(';');
        return commentIndex >= 0 ? asmLine[..commentIndex].TrimEnd() : asmLine;
    }
}