using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ClangSharp;
using ClangSharp.Interop;

namespace Refract.CLI
{
    public class ChunkCreator
    {
        public static List<Chunk> CreateChunksFromCFile(string cFilePath, string asmFilePath)
        {
            var chunks = new List<Chunk>();
            var cLines = File.ReadAllLines(cFilePath);
            var asmLines = File.Exists(asmFilePath) ? File.ReadAllLines(asmFilePath) : Array.Empty<string>();

            using var index = CXIndex.Create();
            var tu = CXTranslationUnit.Parse(index, cFilePath, [], [], CXTranslationUnit_Flags.CXTranslationUnit_None);
            var translationUnit = TranslationUnit.GetOrCreate(tu);
            var cursor = translationUnit.TranslationUnitDecl;

            foreach (var func in cursor.CursorChildren.OfType<FunctionDecl>())
            {
                // TODO: if (func.IsImplicit) continue;
                if (!func.HasBody)
                {
                    continue; // This is just a prototype/declaration, skip it
                }

                var extent = func.Extent;
                extent.Start.GetFileLocation(out _, out var start, out _, out _);
                extent.End.GetFileLocation(out _, out var end, out _, out _);

                var name = func.Name;
                var cCode = string.Join("\n", cLines.Skip((int)start - 1).Take((int)(end - start + 1)));

                // Try to extract the function's address from ASM lines
                var address = "0x000000";
                var functionDeclRegex = new Regex($"; function: ({Regex.Escape(name)}) at (0x[a-fA-F0-9]+)");
                foreach (var line in asmLines)
                {
                    var match = functionDeclRegex.Match(line);
                    if (match.Success)
                    {
                        address = match.Groups[2].Value;
                        break;
                    }
                }

                // var asm = ExtractAssemblyFunction(asmLines, name, address);
                var asm = "";

                var context = $"""
                               Function: {name}
                               Address: {address}
                               Signature: {func.Type}

                               [C]
                               {cCode}

                               [ASM]
                               {asm}
                               """;

                using var md5 = MD5.Create();
                var uid = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(name))).Replace("-", "")
                    .ToLower();

                var chunk = new Chunk
                {
                    id = uid,
                    name = name,
                    address = address,
                    context = context,
                    embedding = null // Will be filled later by the embedder
                };

                chunks.Add(chunk);
            }

            return chunks;
        }

        private static string ExtractAssemblyFunction(string[] asmLines, string functionName, string functionAddress)
        {
            // Try to find the function declaration line
            var functionStartIndex = -1;
            var functionEndAddress = "";

            // Look for the function declaration pattern: "; function: function_name at 0xSTART -- 0xEND"
            var functionDeclPattern = $"; function: {Regex.Escape(functionName)} at {Regex.Escape(functionAddress)}";

            for (int i = 0; i < asmLines.Length; i++)
            {
                if (asmLines[i].Contains(functionDeclPattern))
                {
                    functionStartIndex = i;

                    // Extract the end address if available
                    var endAddressMatch = Regex.Match(asmLines[i], @"-- (0x[a-fA-F0-9]+)");
                    if (endAddressMatch.Success)
                    {
                        functionEndAddress = endAddressMatch.Groups[1].Value;
                    }

                    break;
                }
            }

            if (functionStartIndex == -1)
            {
                // Fallback to the old method if we can't find the declaration
                return string.Join("\n", asmLines.Where(line => line.Contains(functionName))
                    .Select(line => RemoveComments(line)));
            }

            // Collect all lines until we find a new function declaration or hit the end address
            var asmCode = new List<string>();
            asmCode.Add(asmLines[functionStartIndex]); // Add function declaration

            for (int i = functionStartIndex + 1; i < asmLines.Length; i++)
            {
                var line = asmLines[i];

                // Stop if we encounter another function declaration
                if (line.Contains("; function:") && !line.Contains(functionName))
                {
                    break;
                }

                // Skip section comments
                if (line.TrimStart().StartsWith("; section:"))
                {
                    continue;
                }

                // Skip data inside code section comments
                if (line.TrimStart().StartsWith("; data inside code section"))
                {
                    continue;
                }

                // Add the line, but remove inline comments
                asmCode.Add(RemoveComments(line));

                // Stop if we've reached the end address
                if (!string.IsNullOrEmpty(functionEndAddress) &&
                    line.TrimStart().StartsWith(functionEndAddress + ":"))
                {
                    break;
                }
            }

            return string.Join("\n", asmCode);
        }

        private static string RemoveComments(string asmLine)
        {
            // Keep function declaration lines intact
            if (asmLine.TrimStart().StartsWith("; function:"))
            {
                return asmLine;
            }

            // For regular assembly lines, remove comments
            int commentIndex = asmLine.IndexOf(';');
            if (commentIndex >= 0)
            {
                // Keep the instruction and operands, remove the comment
                return asmLine.Substring(0, commentIndex).TrimEnd();
            }

            return asmLine;
        }
    }
}