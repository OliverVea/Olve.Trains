namespace Olve.Engine3D.Logging;

public class NoopLoggingManager : ILoggingManager
{
    public void Log(LogLevel logLevel, string message, string[]? tags = null)
    {
    }

    public void Log(IEnumerable<ResultProblem> resultProblems)
    {
    }

    public void Log(ResultProblem resultProblem)
    {
    }

    public Action<LogMessage>? OnLog { get; set; }
}