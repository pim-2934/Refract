using DotNetBuddy.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refract.CLI.Configs;

namespace Refract.CLI.Installers;

public class DirectoryInstaller(IOptions<GeneralConfig> options) : IInstaller
{
    public void Install(IServiceCollection services)
    {
        Directory.CreateDirectory(options.Value.LogsFolderPath);
        Directory.CreateDirectory(options.Value.SessionsFolderPath);
    }
}