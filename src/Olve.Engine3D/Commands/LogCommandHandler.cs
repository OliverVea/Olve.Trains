using Microsoft.Extensions.Logging;

namespace Olve.Engine3D.Commands;

public class LogCommandHandler(ILogger<LogCommandHandler> logger) : ICommandHandler
{
    private const string LevelKey = "level";
    private const string MessageKey = "message";

    public string Verb => "log";
    public string HelpString => "Emits a log message at the specified level";
    public IReadOnlyList<CommandArgument> Arguments { get; } =
    [
        new(LevelKey, "the log level (debug, info, warn, error)", true),
        new(MessageKey, "the message to log", true),
    ];

    public Result<CommandOutput> Handle(CommandContext context)
    {
        var level = context.Arguments.GetValueOrDefault(LevelKey, "info");
        var message = context.Arguments.GetValueOrDefault(MessageKey, "");

        var logLevel = level.ToLowerInvariant() switch
        {
            "debug" or "dbug" => LogLevel.Debug,
            "info" or "information" => LogLevel.Information,
            "warn" or "warning" => LogLevel.Warning,
            "error" or "fail" => LogLevel.Error,
            "critical" or "crit" => LogLevel.Critical,
            _ => LogLevel.Information,
        };

        logger.Log(logLevel, "{Message}", message);

        return new CommandOutput($"Logged at {logLevel}: {message}");
    }
}
