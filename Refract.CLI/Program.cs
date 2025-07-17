using Refract.CLI.Views;
using Terminal.Gui.App;

string ProjectPath = "../../../../data";
string CPath = Path.Combine(ProjectPath, "pass.c");
string asmFilePath = Path.Combine(ProjectPath, "pass.dsm");

Application.Init();

var view = new TopLevelView();

view.SetAsm(string.Join("\n", File.Exists(asmFilePath) ? File.ReadAllLines(asmFilePath) : []));
view.SetC(string.Join("\n", File.Exists(CPath) ? File.ReadAllLines(CPath) : []));

Application.Run(view);
Application.Shutdown();

// using Microsoft.Extensions.Logging;
//
// namespace Refract.CLI
// {
//     class Program
//     {
//         static readonly string EmbedderUrl = "http://localhost:8081/embed";
//         static readonly string VectorDbUrl = "http://localhost:6333";
//         static readonly string ProjectPath = "../../../../data";
//         static readonly string CPath = Path.Combine(ProjectPath, "pass.c");
//         static readonly string DSMPath = Path.Combine(ProjectPath, "pass.dsm");
//         static readonly string ChunkOutputPath = Path.Combine(ProjectPath, "chunks");
//
//         private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
//         {
//             builder
//                 .SetMinimumLevel(LogLevel.Debug);
//         });
//
//         static async Task Main()
//         {
//             Console.WriteLine("Choose an option:");
//             Console.WriteLine("1. Disassemble + decompile");
//             Console.WriteLine("2. Run embedding + indexing pipeline");
//             Console.WriteLine("3. Ask a question");
//             Console.Write("Selection: ");
//             var input = Console.ReadLine();
//
//             switch (input)
//             {
//                 case "1":
//                     await RunDecompile();
//                     break;
//                 case "2":
//                     await RunPipeline();
//                     break;
//                 case "3":
//                     await AskQuestion();
//                     break;
//                 default:
//                     Console.WriteLine("Invalid input.");
//                     break;
//             }
//         }
//
//         static async Task RunDecompile()
//         {
//             using var process = new System.Diagnostics.Process();
//             process.StartInfo = new System.Diagnostics.ProcessStartInfo
//             {
//                 FileName = "cmd.exe",
//                 Arguments = "/c docker compose run --rm retdec retdec-decompiler.py --cleanup --mode bin --backend-no-debug --backend-no-debug-comments -o /data/pass.c /data/pass",
//                 RedirectStandardOutput = true,
//                 RedirectStandardError = true,
//                 UseShellExecute = false,
//                 CreateNoWindow = true
//             };
//     
//             process.Start();
//     
//             // Read output asynchronously
//             var output = await process.StandardOutput.ReadToEndAsync();
//             var error = await process.StandardError.ReadToEndAsync();
//     
//             await process.WaitForExitAsync();
//     
//             if (!string.IsNullOrEmpty(output))
//                 Console.WriteLine(output);
//     
//             if (!string.IsNullOrEmpty(error))
//                 Console.WriteLine(error);
//     
//             Console.WriteLine($"Decompilation process exited with code: {process.ExitCode}");
//         }
//
//         static async Task RunPipeline()
//         {
//             var chunks = ChunkCreator.CreateChunksFromCFile(CPath, DSMPath);
//             Console.WriteLine($"Created {chunks.Count} chunks from C file");
//
//             var embeddingService = new EmbeddingService(EmbedderUrl);
//             chunks = await embeddingService.EmbedChunksAsync(chunks);
//             Console.WriteLine("All chunks embedded successfully");
//
//             var vectorDbService = new VectorDbService(VectorDbUrl, ChunkOutputPath);
//             await vectorDbService.ProcessChunksAsync(chunks);
//
//             Console.WriteLine("✅ All chunks embedded and indexed.");
//         }
//
//         static async Task AskQuestion()
//         {
//             Console.Write("Enter your question: ");
//             var question = Console.ReadLine();
//
//             var ragService = new RagService(VectorDbUrl, EmbedderUrl, _loggerFactory.CreateLogger<RagService>());
//             var response = await ragService.AskAsync(question);
//
//             Console.WriteLine("--- Answer ---");
//             Console.WriteLine(response);
//         }
//     }
// }