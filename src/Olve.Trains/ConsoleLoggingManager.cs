using Olve.Logging;

namespace Olve.Trains;

/// <summary>
/// LoggingManager that bridges Olve.Logging to Console output
/// while also maintaining in-memory logging for the debug server.
/// </summary>
public class ConsoleLoggingManager : ILoggingManager
{
    private readonly InMemoryLoggingManager _inMemoryLogger;

    public ConsoleLoggingManager(InMemoryLoggingManager inMemoryLogger)
    {
        _inMemoryLogger = inMemoryLogger;
    }

    public LogsUpdatedEvent LogsUpdatedEvent => _inMemoryLogger.LogsUpdatedEvent;

    public void Log(LogMessage log)
    {
        // Log to in-memory for debug server
        _inMemoryLogger.Log(log);

        // Also log to console
        var levelStr = FormatLogLevel(log.Level);
        var tagsStr = log.Tags is { Length: > 0 } ? $"[{string.Join(", ", log.Tags)}] " : "";
        Console.WriteLine($"{levelStr}: {tagsStr}{log.Message}");
    }

    public void Log(LogLevel logLevel, string message, string[]? tags = null)
    {
        // Log to in-memory for debug server
        _inMemoryLogger.Log(logLevel, message, tags);

        // Also log to console
        var levelStr = FormatLogLevel(logLevel);
        var tagsStr = tags is { Length: > 0 } ? $"[{string.Join(", ", tags)}] " : "";
        Console.WriteLine($"{levelStr}: {tagsStr}{message}");
    }

    public void Log(IEnumerable<ResultProblem> resultProblems)
    {
        // Log to in-memory for debug server
        _inMemoryLogger.Log(resultProblems);

        // Also log to console
        foreach (var problem in resultProblems)
        {
            Console.WriteLine($"fail: {problem.ToDebugString()}");
        }
    }

    public Result<GetLogsResponse> GetLogs(GetLogsRequest request)
    {
        return _inMemoryLogger.GetLogs(request);
    }

    private static string FormatLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Debug => "dbug",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            _ => "info"
        };
    }
}
