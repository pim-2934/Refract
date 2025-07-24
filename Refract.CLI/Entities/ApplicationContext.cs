using Microsoft.Extensions.Options;
using Refract.CLI.Configs;
using Refract.CLI.Utilities;

namespace Refract.CLI.Entities;

public class ApplicationContext(IOptions<GeneralConfig> options)
{
    public Session? Session { get; private set; }
    public bool IsActiveSession => Session is not null;

    public async Task StartSession(string targetBinaryPath)
    {
        var fileBytes = await File.ReadAllBytesAsync(targetBinaryPath);
        var crc32 = HashUtilities.CalculateCrc32(fileBytes);

        Session = new Session(crc32.ToString("X8"), options.Value.SessionsFolderPath);

        if (!Directory.Exists(Session.SessionPath))
            Directory.CreateDirectory(Session.SessionPath);

        File.Copy(targetBinaryPath, Session.BinaryFilePath, true);

        Session.Load();
    }
}