using DotNetBuddy.Domain;

namespace Refract.CLI.Configs;

public class GeneralConfig : IConfig
{
    public string SessionsFolderPath { get; set; } = "sessions";
    public string LogsFolderPath { get; set; } = "logs";
}