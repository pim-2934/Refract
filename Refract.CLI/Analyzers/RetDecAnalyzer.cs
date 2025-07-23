namespace Refract.CLI.Analyzers;

public class RetDecAnalyzer : IBinaryAnalyzer
{
    public Task<string> DecompileToCAsync(Session session)
    {
        throw new NotImplementedException();
    }

    public Task<string> DisassembleToAsmAsync(Session session)
    {
        throw new NotImplementedException();
    }

    public Task<string> DumpHexAsync(Session session)
    {
        throw new NotImplementedException();
    }
}