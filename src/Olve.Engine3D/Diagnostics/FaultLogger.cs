using Microsoft.Extensions.Logging;

namespace Olve.Engine3D.Diagnostics;

public sealed class FaultLogger(ILogger<FaultLogger> logger)
{
    private readonly Dictionary<string, int> _failedFrames = new();

    public void LogFault(string source, ResultProblemCollection problems)
    {
        _failedFrames.TryGetValue(source, out var count);
        _failedFrames[source] = count + 1;

        if (count > 0)
        {
            return;
        }

        logger.LogError("{Source} failed; continuing and suppressing repeats until it recovers:{NewLine}{Problems}",
            source, Environment.NewLine, string.Join(Environment.NewLine, problems.Select(p => p.ToDebugString())));
    }

    public void LogSuccess(string source)
    {
        if (_failedFrames.Count == 0 || !_failedFrames.Remove(source, out var count))
        {
            return;
        }

        logger.LogInformation("{Source} recovered after {FailedFrames} failed frame(s)", source, count);
    }
}
