using Refract.CLI.Entities;

namespace Refract.CLI;

public interface IBinaryAnalyzer
{
    public Task<string> DecompileToCAsync(Session session);
    public Task<string> DisassembleToAsmAsync(Session session);
    public Task<string> DumpHexAsync(Session session);
}