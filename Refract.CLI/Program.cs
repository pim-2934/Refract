using Microsoft.Extensions.Logging;
using Refract.CLI.Services;
using Refract.CLI.Views;
using Serilog;
using Terminal.Gui.App;

const string embedderUrl = "http://localhost:8081/embed";
const string vectorDbUrl = "http://localhost:6333";

Directory.CreateDirectory("logs");
Directory.CreateDirectory("sessions");

// Setup Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .Enrich.FromLogContext()
    .WriteTo.File(
        path: Path.Combine("logs", $"app-{DateTime.Now:yyyy-MM-dd}.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        encoding: System.Text.Encoding.UTF8,
        buffered: false,
        flushToDiskInterval: TimeSpan.FromSeconds(1),
        shared: true)
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddSerilog(Log.Logger, dispose: false)
        .SetMinimumLevel(LogLevel.Trace);
});

// Setup Application
Logging.Logger = loggerFactory.CreateLogger("Global Logger");
Application.Init();

// Run application
try
{
    Application.Run(new MainView(
        new DecompileService(loggerFactory.CreateLogger<DecompileService>()),
        new RagService(vectorDbUrl, embedderUrl, loggerFactory.CreateLogger<RagService>()),
        new EmbeddingService(embedderUrl, loggerFactory.CreateLogger<EmbeddingService>()),
        new VectorDbService(vectorDbUrl, loggerFactory.CreateLogger<VectorDbService>())
    ));
}
finally
{
    Application.Shutdown();
}