namespace Olve.Engine3D.Logging;

public interface ILoggingManager
{
    void Log(LogLevel logLevel, string message, string[]? tags = null);
    void Log(IEnumerable<ResultProblem> resultProblems);
    void Log(ResultProblem resultProblem);

    Action<LogMessage>? OnLog { get; set; }
}