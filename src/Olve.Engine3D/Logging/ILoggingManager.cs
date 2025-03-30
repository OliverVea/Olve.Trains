namespace Olve.Engine3D.Logging;


public enum LogLevel
{
    Info,
    Warning,
    Error,
    Critical
}

public readonly record struct LogMessage(LogLevel Level, string Message)
{
    public string? SourcePath { get; init; }
    public int? SourceLine { get; init; }
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;

    public string[]? Tags { get; init; }
}

public interface ILoggingManager
{
    void Log(LogLevel logLevel, string message, string[]? tags = null);
    void Log(IEnumerable<ResultProblem> resultProblems);
    void Log(ResultProblem resultProblem);

    Action<LogMessage>? OnLog { get; set; }
}