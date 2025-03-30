using Olve.Engine3D;
using Olve.Engine3D.Logging;
using Olve.Results;

namespace Olve.Trains.Scenes.Console;

public class ConsoleLoggingManager : ILoggingManager
{
    public void Log(LogLevel logLevel, string message, string[]? tags = null)
    {
        LogMessage logMessage = new(logLevel, message)
        {
            Tags = tags
        };

        Log(logMessage);
    }

    private void Log(LogMessage logMessage)
    {
        OnLog?.Invoke(logMessage);
    }

    public void Log(IEnumerable<ResultProblem> resultProblems)
    {
        foreach (var resultProblem in resultProblems)
        {
            Log(resultProblem);
        }
    }

    public void Log(ResultProblem resultProblem)
    {
        var logLevel = resultProblem.GetLogLevel();

        LogMessage logMessage = new(logLevel, resultProblem.Message)
        {
            SourcePath = resultProblem.OriginInformation.FilePath,
            SourceLine = resultProblem.OriginInformation.LineNumber,
            Tags = resultProblem.Tags
        };

        Log(logMessage);
    }

    public Action<LogMessage>? OnLog { get; set; }
}

public static class ResultProblemExtensions
{
    public static LogLevel GetLogLevel(this ResultProblem resultProblem)
    {
        if (resultProblem.Severity < ProblemSeverities.Warning)
        {
            return LogLevel.Info;
        }

        if (resultProblem.Severity < ProblemSeverities.Error)
        {
            return LogLevel.Warning;
        }

        if (resultProblem.Severity < ProblemSeverities.Critical)
        {
            return LogLevel.Error;
        }

        return LogLevel.Critical;
    }
}