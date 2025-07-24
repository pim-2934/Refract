namespace Refract.CLI;

public interface ITalker
{
    Task<string> AskAsync(string question, string context);
}