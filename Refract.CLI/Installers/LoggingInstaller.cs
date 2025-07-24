using DotNetBuddy.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refract.CLI.Configs;
using Serilog;

namespace Refract.CLI.Installers;

public class LoggingInstaller(IOptions<GeneralConfig> options) : IInstaller
{
    public void Install(IServiceCollection services)
    {
        services.AddLogging(x =>
        {
            x.ClearProviders();
            x.AddSerilog(new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: Path.Combine(options.Value.LogsFolderPath, $"app-{DateTime.Now:yyyy-MM-dd}.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    encoding: System.Text.Encoding.UTF8,
                    buffered: false,
                    flushToDiskInterval: TimeSpan.FromSeconds(1),
                    shared: true)
                .CreateLogger(), dispose: true);
        });
    }
}