using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Olve.Engine3D.AssetPipeline;

public class CustomFormatter() : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "custom";

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var (level, color) = logEntry.LogLevel switch
        {
            LogLevel.Trace => ("TRACE", "\u001b[90m"),  // Gray
            LogLevel.Debug => ("DEBUG", "\u001b[94m"),  // Blue
            LogLevel.Information => ("INFO", "\u001b[32m"), // Green
            LogLevel.Warning => ("WARN", "\u001b[33m"), // Yellow
            LogLevel.Error => ("ERROR", "\u001b[31m"), // Red
            LogLevel.Critical => ("CRIT", "\u001b[35m"), // Magenta
            _ => ("UNKNOWN", "\u001b[0m") // Reset
        };

        var reset = "\u001b[0m"; // Reset color after message

        textWriter.WriteLine($"{color}[{level}] {timestamp}: {logEntry.Formatter(logEntry.State, logEntry.Exception)}{reset}");
    }
}